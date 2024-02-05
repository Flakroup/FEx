using FEx.Basics.Utilities;
using FEx.Extensions.Collections.Lists;
using FEx.LiteDBx.Abstractions.Interfaces;
using FEx.LiteDBx.Enums;
using FEx.LiteDBx.Extensions;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace FEx.LiteDBx.Services;

public sealed class LiteDBService : ILiteDBService
{
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IFileLocalStorageService _fileLocalStorageService;
    private readonly IClearCache[] _clearCaches;

    private ILiteRepository Context => _databaseProvider.Repository;
    private ExtendedReaderWriterLockSlim DbLock => _databaseProvider.DbLock;

    public LiteDBService(IDatabaseProvider databaseProvider,
                         IFileLocalStorageService fileLocalStorageService,
                         IClearCache[] clearCaches)
    {
        _databaseProvider = databaseProvider;
        _fileLocalStorageService = fileLocalStorageService;
        _clearCaches = clearCaches;
    }

    /// <summary>
    ///     Caches object
    /// </summary>
    /// <param name="item">Object to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    public void Add<T>(T item) where T : ICacheableItem
    {
        DbLock.Write(() => Context.Insert(item));
    }

    /// <summary>
    ///     Caches the range of objects
    /// </summary>
    /// <param name="items">Collection of objects to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    public void Add<T>(IEnumerable<T> items) where T : ICacheableItem
    {
        var deferredList = items.ToList();
        DbLock.Write(() => Context.Insert(deferredList));
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
            DbLock.WriteWithResult(() => Context.Update(item));
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
        DbLock.WriteWithResult(() => Context.Delete<T>(id));
    }

    /// <summary>
    ///     Removes cached objects
    /// </summary>
    /// <param name="items">Objects to be removed</param>
    /// <typeparam name="T">Type of objects to be removed</typeparam>
    public void Delete<T>(IEnumerable<T> items) where T : ICacheableItem
    {
        DbLock.Write(() =>
        {
            foreach (T item in items)
                Context.Delete<T>(item.LocalStorageId);
        });
    }

    /// <summary>
    ///     Removes cached objects that meet criteria of the predicate
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of objects to be removed</typeparam>
    public void Delete<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem
    {
        DbLock.WriteWithResult(() => InternalDelete(predicate));
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
            predicate ??= GetTrueExpression<T>();

            return DbLock.ReadWithResult(() => Context.FirstOrDefault(predicate));
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

            return DbLock.ReadWithResult(() => Context.Fetch(predicate));
        }
        catch
        {
            Delete<T>();

            return Enumerable.Empty<T>().ToList().AsReadOnly();
        }
    }

    /// <summary>
    ///     Calls the ClearCache method of inheritors of IClearCache for the selected Cached Type
    /// </summary>
    /// <param name="clearCacheReason">Clear Cache Reason</param>
    public void ClearCache(ClearCacheReason clearCacheReason)
    {
        if (_clearCaches.IsNullOrEmptyList())
            return;

        var cleanupCandidates = _clearCaches.Where(service => service.ClearCacheReason.HasFlagFast(clearCacheReason))
            .ToList();

        foreach (IClearCache cleanupCandidate in cleanupCandidates)
            cleanupCandidate.ClearCache();
    }

    public ICachedFile CacheFile(IDownloadResult downloadResult) =>
        DbLock.WriteWithResult(() => _fileLocalStorageService.CacheFile(downloadResult));

    public ICachedFile GetCachedFile(Uri fileUrl) =>
        DbLock.ReadWithResult(() => _fileLocalStorageService.GetCachedFile(fileUrl));

    public void Replace<T>(T item) where T : ICacheableItem
    {
        DbLock.Write(() =>
        {
            InternalDelete<T>();
            Context.Insert(item);
        });
    }

    private static Expression<Func<T, bool>> GetTrueExpression<T>()
    {
        Type type = typeof(T);

        return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), Expression.Parameter(type, "_"));
    }

    private bool InternalDelete<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem
    {
        predicate ??= GetTrueExpression<T>();

        //LiteDB issue workaround https://github.com/mbdavid/LiteDB/issues/1940#issuecomment-961784366
        int? documentsCount = CollectionCount(predicate);

        return documentsCount is null or 0
               || Context.Database.GetCollection<T>().DeleteMany(predicate) == documentsCount;
    }

    private int? CollectionCount<T>(Expression<Func<T, bool>> predicate) where T : ICacheableItem =>
        Context.Database.CollectionExists(typeof(T).Name)
            ? Context.Database.GetCollection<T>().Count(predicate)
            : null;
}