using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Utilities;
using FEx.PersistentStorage.Abstractions;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace FEx.PersistentStorage;

public class LocalStorageService : ILocalStorageService
{
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IFileLocalStorageService _fileLocalStorageService;
    private readonly IFExLogger _logger;

    protected ILiteRepository Context => _databaseProvider.Repository;
    protected ExtendedReaderWriterLockSlim DbLock => _databaseProvider.DbLock;

    public LocalStorageService(IDatabaseProvider databaseProvider,
                               IFileLocalStorageService fileLocalStorageService,
                               IFExLogger logger)
    {
        _databaseProvider = databaseProvider;
        _fileLocalStorageService = fileLocalStorageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public IFExCachedFile CacheFile(IFExDownloadResult downloadResult) =>
        DbLock.WriteWithResult(() => _fileLocalStorageService.CacheFile(downloadResult));

    /// <inheritdoc />
    public IFExCachedFile GetCachedFile(Uri fileUrl) =>
        DbLock.ReadWithResult(() => _fileLocalStorageService.GetCachedFile(fileUrl));

    /// <inheritdoc />
    public void UpdateFile(IFExCachedFile cachedFile) =>
        DbLock.Write(() => _fileLocalStorageService.UpdateFile(cachedFile));

    /// <inheritdoc />
    public void DeleteExpiredFiles() => DbLock.Write(() => _fileLocalStorageService.DeleteExpiredFiles());

    /// <inheritdoc />
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem =>
        DbLock.ReadWithResult(() =>
        {
            try
            {
                return predicate is null
                    ? Context.Database.GetCollection<T>().FindAll().ToList().AsReadOnly()
                    : Context.Fetch(predicate).AsReadOnly();
            }
            catch (InvalidCastException ex)
            {
                if (Debugger.IsAttached)
                    _logger.Error(ex);

                return new List<T>().AsReadOnly();
            }
        });

    /// <inheritdoc />
    public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem
    {
        predicate ??= GetTrueExpression<T>();

        return DbLock.ReadWithResult(() => Context.FirstOrDefault(predicate));
    }

    /// <inheritdoc />
    public BsonValue Insert<T>(T item) where T : ICacheableItem => DbLock.WriteWithResult(() => Context.Insert(item));

    /// <inheritdoc />
    public void Insert<T>(IEnumerable<T> items) where T : ICacheableItem => DbLock.Write(() => Context.Insert(items));

    /// <inheritdoc />
    public bool Update<T>(T item) where T : ICacheableItem => DbLock.WriteWithResult(() => Context.Update(item));

    /// <inheritdoc />
    public bool Upsert<T>(T item, Expression<Func<T, bool>> predicate = null) where T : ICacheableItem =>
        DbLock.WriteWithResult(() =>
        {
            if (predicate is null)
                return Context.Upsert(item);

            InternalDeleteAll(predicate);
            Context.Insert(item);

            return true;
        });

    /// <inheritdoc />
    public void Upsert<T>(IEnumerable<T> items, Expression<Func<T, bool>> predicate = null) where T : ICacheableItem =>
        DbLock.Write(() =>
        {
            if (predicate is null)
            {
                Context.Upsert(items);

                return;
            }

            InternalDeleteAll(predicate);
            Context.Insert(items);
        });

    /// <inheritdoc />
    public bool Delete<T>(ObjectId id) where T : ICacheableItem => DbLock.WriteWithResult(() => Context.Delete<T>(id));

    /// <inheritdoc />
    public bool Delete<T>(IEnumerable<T> items) where T : ICacheableItem =>
        DbLock.WriteWithResult(() => items.All(item => Context.Delete<T>(item.LocalStorageId)));

    /// <inheritdoc />
    public bool DeleteAll<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem =>
        DbLock.WriteWithResult(() => InternalDeleteAll(predicate));

    /// <inheritdoc />
    public void ReplaceWith<T>(IEnumerable<T> items) where T : ICacheableItem
    {
        DbLock.Write(() =>
        {
            InternalDeleteAll<T>();
            Context.Insert(items);
        });
    }

    /// <inheritdoc />
    public BsonValue ReplaceWith<T>(T item) where T : ICacheableItem =>
        DbLock.WriteWithResult(() =>
        {
            InternalDeleteAll<T>();

            return Context.Insert(item);
        });

    /// <summary>
    /// Generates a lambda expression that always returns true for any type.
    /// </summary>
    /// <typeparam name="T">The type for which to generate the expression.</typeparam>
    /// <returns>A lambda expression that always evaluates to true.</returns>
    private static Expression<Func<T, bool>> GetTrueExpression<T>()
    {
        Type type = typeof(T);
        ParameterExpression parameter = Expression.Parameter(type);

        return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), parameter);
    }

    private bool InternalDeleteAll<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem
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