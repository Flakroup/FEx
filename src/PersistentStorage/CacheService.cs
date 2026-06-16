using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using FEx.PersistentStorage.Abstractions.Extensions;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace FEx.PersistentStorage;

public sealed class CacheService : ICacheService
{
    private readonly ILocalStorageService _localStorageService;
    private readonly IFExLogger _logger;

    public CacheService(ILocalStorageService localStorageService, IFExLogger logger)
    {
        _localStorageService = localStorageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public BsonValue Add<T>(T item) where T : ICacheableItem => _localStorageService.Insert(item);

    /// <inheritdoc />
    public void Add<T>(IEnumerable<T> items) where T : ICacheableItem => _localStorageService.Insert(items);

    /// <inheritdoc />
    public bool Update<T>(T item) where T : ICacheableItem
    {
        try
        {
            return _localStorageService.Update(item);
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
                _logger.Error(ex);

            Delete((Expression<Func<T, bool>>)null);
            Add(item);

            return true;
        }
    }

    /// <inheritdoc />
    public bool Upsert<T>(T item, Expression<Func<T, bool>> predicate) where T : ICacheableItem
    {
        try
        {
            return _localStorageService.Upsert(item, predicate);
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
                _logger.Error(ex);

            Delete((Expression<Func<T, bool>>)null);
            Add(item);

            return true;
        }
    }

    /// <inheritdoc />
    public void Upsert<T>(IEnumerable<T> items, Expression<Func<T, bool>> predicate) where T : ICacheableItem
    {
        try
        {
            _localStorageService.Upsert(items, predicate);
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
                _logger.Error(ex);

            Delete((Expression<Func<T, bool>>)null);
            Add(items);
        }
    }

    /// <inheritdoc />
    public bool Delete<T>(ObjectId id) where T : ICacheableItem => _localStorageService.Delete<T>(id);

    /// <inheritdoc />
    public bool Delete<T>(IEnumerable<T> items) where T : ICacheableItem => _localStorageService.Delete(items);

    /// <inheritdoc />
    public bool Delete<T>(Expression<Func<T, bool>> predicate) where T : ICacheableItem =>
        _localStorageService.DeleteAll(predicate);

    /// <inheritdoc />
    public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : ICacheableItem
    {
        try
        {
            return _localStorageService.FirstOrDefault(predicate);
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
                _logger.Error(ex);

            Delete((Expression<Func<T, bool>>)null);

            return default;
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<T> Get<T>(Expression<Func<T, bool>> predicate) where T : ICacheableItem
    {
        try
        {
            return _localStorageService.GetAll(predicate);
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
                _logger.Error(ex);

            Delete((Expression<Func<T, bool>>)null);

            return Enumerable.Empty<T>().ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public void ReplaceWith<T>(IEnumerable<T> items) where T : ICacheableItem =>
        _localStorageService.ReplaceWith(items);

    /// <inheritdoc />
    public BsonValue ReplaceWith<T>(T item) where T : ICacheableItem => _localStorageService.ReplaceWith(item);

    /// <inheritdoc />
    public void ClearCache(ClearCacheReason clearCacheReason)
    {
        var cleanupCandidates = FExServiceProvider.GetAll<IClearCache>()
            .Where(service => service.ClearCacheReason.HasFlagFast(clearCacheReason))
            .OrderByDescending(static service => service.ClearCachePriority)
            .ToList();

        foreach (var cleanupCandidate in cleanupCandidates)
            cleanupCandidate.ClearCache();
    }

    public IFExCachedFile CacheFile(IFExDownloadResult downloadResult) =>
        _localStorageService.CacheFile(downloadResult);

    public IFExCachedFile GetCachedFile(Uri fileUrl) => _localStorageService.GetCachedFile(fileUrl);

    public void UpdateFile(IFExCachedFile cachedFile) => _localStorageService.UpdateFile(cachedFile);

    public void DeleteExpiredFiles() => _localStorageService.DeleteExpiredFiles();
}