using FEx.Utilities.Basics;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace FEx.LiteDBx;

public abstract class LiteDBService : IDisposable
{
    private readonly ILiteRepository _context;
    private readonly ExtendedReaderWriterLockSlim _lock;

    private bool _isDisposed;

    protected LiteDBService(ILiteRepository context)
    {
        _lock = new();
        _context = context;
    }

    private static Expression<Func<T, bool>> GetTrueExpression<T>()
    {
        Type type = typeof(T);
        return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), Expression.Parameter(type, "_"));
    }

    /// <summary>
    ///     Caches object
    /// </summary>
    /// <param name="item">Object to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    public void Add<T>(T item) where T : ICacheableItem
    {
        _lock.Write(() => WrapInTransaction(() => _context.Insert(item)));
    }

    /// <summary>
    ///     Caches the range of objects
    /// </summary>
    /// <param name="items">Collection of objects to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    public void Add<T>(IEnumerable<T> items) where T : ICacheableItem
    {
        var deferredList = items.ToList();
        _lock.Write(() => WrapInTransaction(() => _context.Insert(deferredList)));
    }

    /// <summary>
    ///     Updates an existing object. Removes the entire collection.
    /// </summary>
    /// <param name="item">Object to be cached</param>
    /// <typeparam name="T">Type of an object to be cached</typeparam>
    /// <remarks>
    ///     The entire collection of cached objects of specified type will be removed in the case of exception caught.
    ///     Item will be added anyways.
    /// </remarks>
    public void Update<T>(T item) where T : ICacheableItem
    {
        try
        {
            _lock.WriteWithResult(() => WrapInTransaction(() => _context.Update(item)));
        }
        catch
        {
            Delete<T>();
            Add(item);
        }
    }

    /// <summary>
    ///     Removes cached object of specified type
    /// </summary>
    /// <param name="id">The local storage id of the object to be removed</param>
    /// <typeparam name="T">Type of an object to be removed</typeparam>
    public void Delete<T>(ObjectId id) where T : ICacheableItem
    {
        _lock.WriteWithResult(() => WrapInTransaction(() => _context.Delete<T>(id)));
    }

    /// <summary>
    ///     Removes cached objects
    /// </summary>
    /// <param name="items">Objects to be removed</param>
    /// <typeparam name="T">Type of objects to be removed</typeparam>
    public void Delete<T>(IEnumerable<T> items) where T : ICacheableItem
    {
        _lock.Write(() => WrapInTransaction(() =>
        {
            foreach (T item in items)
                _context.Delete<T>(item.LocalStorageId);
        }));
    }

    /// <summary>
    ///     Removes cached objects that meet criteria of the predicate
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of objects to be removed</typeparam>
    public void Delete<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem
    {
        _lock.WriteWithResult(() =>
        {
            if (!_context.Database.CollectionExists(typeof(T).Name))
                return true;

            //LiteDB issue workaround https://github.com/mbdavid/LiteDB/issues/1940#issuecomment-961784366
            int documentsCount = _context.Database.GetCollection<T>()
                .Count();

            predicate ??= GetTrueExpression<T>();
            return WrapInTransaction(() => _context.Database.GetCollection<T>()
                                               .DeleteMany(predicate)
                                           == documentsCount);
        });
    }

    /// <summary>
    ///     Tries to find an object in cache that meets the requirements of the predicate
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of an object to be found</typeparam>
    /// <returns>
    ///     Returns the cached object that fits the requirements of the predicate parameter. Returns "default" when object
    ///     not found of exception caught.
    /// </returns>
    /// <remarks>The entire collection of cached objects of specified type will be removed in the case of exception caught.</remarks>
    public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem
    {
        try
        {
            return _lock.ReadWithResult(() => _context.FirstOrDefault(predicate));
        }
        catch
        {
            Delete<T>();
            return default;
        }
    }

    /// <summary>
    ///     The entire collection of cached objects of specified type matching predicate or all of them if predicate is null.
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of objects to be retrieved</typeparam>
    /// <returns>Returns all cached objects of specified type matching predicate or all of them if predicate is null</returns>
    /// <remarks>The entire collection of cached objects of specified type will be removed in the case of exception caught.</remarks>
    public IReadOnlyList<T> Get<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem
    {
        try
        {
            predicate ??= GetTrueExpression<T>();
            return _lock.ReadWithResult(() => _context.Fetch(predicate));
        }
        catch
        {
            Delete<T>();
            return Enumerable.Empty<T>()
                .ToList()
                .AsReadOnly();
        }
    }

    private void WrapInTransaction(Action action)
    {
        _context.Database.BeginTrans();
        try
        {
            action();
            _context.Database.Commit();
        }
        catch (Exception)
        {
            _context.Database.Rollback();
            throw;
        }
    }

    private T WrapInTransaction<T>(Func<T> func)
    {
        _context.Database.BeginTrans();
        try
        {
            T result = func();
            _context.Database.Commit();
            return result;
        }
        catch (Exception)
        {
            _context.Database.Rollback();
            throw;
        }
    }

    #region IDisposable

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            _context.Dispose();

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    #endregion
}