using DynamicData;
using DynamicData.Binding;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Collections.Concurrent;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.EFCore.Collections;

/// <summary>
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
/// <typeparam name="TDbCtx"></typeparam>
/// <remarks>Requires <c>Initialize();</c> call in .ctor</remarks>
public abstract class SynchronizedDictionary<TKey, TValue, TDbCtx> : AsyncInitializable, IReadOnlyCollection<TValue>
    where TKey : notnull, IEquatable<TKey> where TValue : class, INotifyPropertyChanged where TDbCtx : DbContext
{
    protected readonly IEFCoreDatabaseBackedService<TDbCtx> _dbSrv;
    private readonly Func<TValue, IObservable<object>>[]? _observables;
    private bool _isDisposed;
    private string[] _observedProperties;
    private IDisposable? _cacheSubscription;
    private Func<TValue, TKey>? _keyRetriver;

    public ConcurrentHashSet<TKey> Index { get; }
    public bool UseIndex { get; protected set; }

    public int Count => Cache.Count;

    public TValue this[TKey key] => Cache.Lookup(key).Value;

    protected bool HasCachedAll { get; set; }
    protected string KeyPropertyName { get; }
    protected SourceCache<TValue, TKey> Cache { get; }
    protected ConcurrentDictionary<string, Task<bool>> CacheTasks { get; }
    protected SemaphoreSlim CacheHandlerSemaphore { get; }

    protected SynchronizedDictionary(IEFCoreDatabaseBackedService<TDbCtx> dbService,
                                     string keyPropertyName,
                                     Func<TValue, IObservable<object>>[]? observables = null,
                                     params string[] observedProperties)
        : base(dbService)
    {
        KeyPropertyName = keyPropertyName;
        _dbSrv = dbService;
        _observables = observables;
        _observedProperties = observedProperties;
        CacheHandlerSemaphore = new(1, 1);
        Cache = new(KeyRetriver);
        Index = [];
        CacheTasks = new();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<TValue> GetEnumerator() => Cache.Items.GetEnumerator();

    public async Task<TValue?> GetOrAddValueAsync(TKey key, bool addNew = true, IDictionary<string, object>? param = null)
    {
        var optional = Cache.Lookup(key);

        return optional.HasValue
            ? optional.Value
            : await FindExistingValueOrAddNewAsync(key, addNew, param);
    }

    public async Task<TimeSpan> CacheAllAsync(HashSet<TKey>? keys = null)
    {
        var sw = Stopwatch.StartNew();
        await _dbSrv.RunTaskInDbContextAsync(ctx => CacheAllAsync(ctx, keys));

        if (keys is null || keys.Count == 0
            || Index.UnorderedSequenceEqual(keys))
            HasCachedAll = true;

        sw.Stop();

        return sw.Elapsed;
    }

    public bool Remove(TValue value) => Remove(KeyRetriver(value));

    public bool Remove(TKey key)
    {
        var optional = Cache.Lookup(key);

        if (!optional.HasValue)
            return false;

        Cache.Remove(key);

        return true;
    }

    public async Task RemoveWhereAsync(Func<TKey, TValue, bool> func) =>
        await ForAllAsync((k, v) => RemoveIfMatch(k, v, func));

    public async Task ForAllTaskAsync(Func<TKey, TValue, Task> func) =>
        await Cache.KeyValues.WithWhenAllTasksAsync(x => func(x.Key, x.Value));

    public async Task<T[]> ForAllTaskAsync<T>(Func<TKey, TValue, Task<T>> func) =>
        await Cache.KeyValues.WithWhenAllTasksAsync(x => func(x.Key, x.Value));

    public async Task<T[]> ForAllFuncAsync<T>(Func<TKey, TValue, T> func) =>
        await Cache.KeyValues.WithWhenAllAsync(x => func(x.Key, x.Value));

    public async Task ForAllAsync(Action<TKey, TValue> func) =>
        await Cache.KeyValues.WithWhenAllAsync(x => func(x.Key, x.Value));

    public async Task<T[]> ForAllAsync<T>(Func<TKey, TValue, T> func) =>
        await Cache.KeyValues.WithWhenAllAsync(x => func(x.Key, x.Value));

    public async Task<bool> ContainsKeyAsync(TKey key)
    {
        var hasKey = Cache.Keys.Contains(key);

        if (hasKey || HasCachedAll)
            return hasKey;

        return UseIndex
            ? IndexContains(key)
            : await _dbSrv.RunTaskInDbContextAsync(dbContext => ExistsInDbAsync(dbContext, key));
    }

    public async Task<bool> CacheIsEmptyAsync() =>
        !Cache.Items.Any() && !await _dbSrv.RunTaskInDbContextAsync(ctx => DbSetAccessor(ctx).AnyAsync());

    public void AddOrUpdateValue(TValue value) => Cache.AddOrUpdate(value);

    public async Task EnsureKeysIndexAsync() => await _dbSrv.RunTaskInDbContextAsync(EnsureKeysIndexAsync);

    public async Task WaitForCacheTasksAsync()
    {
        while (!CacheTasks.IsEmpty)
        {
            try
            {
                await Task.WhenAll(CacheTasks.Values);
            }
            catch
            {
                //ignored
            }
            finally
            {
                await Task.Delay(200);
            }
        }
    }

    protected abstract Expression<Func<TValue, TKey>> RetriveKey();

    // VSTHRD200: DbSetAccessor returns a DbSet<T> (which implements IAsyncEnumerable) but is a
    // synchronous accessor, not an async method - an "Async" suffix would be misleading.
#pragma warning disable VSTHRD200
    protected abstract DbSet<TValue> DbSetAccessor(TDbCtx ctx);
#pragma warning restore VSTHRD200

    protected abstract TValue GetNew(TKey key, IDictionary<string, object>? param = null);

    protected virtual async Task OnChangesDetectedAsync(ICollection<ChangeInfo<TKey, TValue>> changes) =>
        await _dbSrv.RunTaskInDbContextAsync(ctx => SaveCacheChangesAsync(ctx, changes));

    protected virtual IQueryable<TValue> IncludeInEntity(IQueryable<TValue> query) => query;

    // ReSharper disable UnusedParameter.Global
    protected virtual Task<TValue> LoadEntityAsync(TDbCtx ctx, TValue entity) => Task.FromResult(entity);
    // ReSharper restore UnusedParameter.Global

    protected virtual void OnRemovedFromCache(TValue removedValue)
    {
    }

    protected virtual void OnRetrievedNew(TValue value)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        var mappedProperties = _dbSrv.Mappings[typeof(TValue).FullName.Guard(nameof(TValue))].Properties;

        _observedProperties = _observedProperties is null
            ? [.. mappedProperties]
            : [.. _observedProperties, .. mappedProperties];

        var cacheObservable =
            Cache.Connect().AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(_observedProperties));

        if (_observables?.Any() == true)
            cacheObservable =
                _observables.Aggregate(cacheObservable, (current, o) => current.AutoRefreshOnObservable(o));

        _cacheSubscription?.Dispose();

        _cacheSubscription = cacheObservable.Buffer(TimeSpan.FromMilliseconds(100))
            .Where(x => x.Count > 0 && x.Any(c => c.Count > 0))
            .Select(x =>
            {
                var changes = x.SelectMany(c => c)
                    .Where(c => c.Reason is not ChangeReason.Moved
                                && (c.Reason != ChangeReason.Add || !Index.Contains(c.Key)))
                    .ToList();

                if (changes.Count == 0)
                    return [];

                var distinctChanges = DistinctChanges(changes);

                return new ChangeSet<TValue, TKey>(distinctChanges);
            })
            .NotEmpty()
            .SubscribeTask((x, _) => HandleCacheAsync(x));
    }

    protected TKey KeyRetriver(TValue value) => (_keyRetriver ??= RetriveKey().Compile()).Invoke(value);

    protected T? GetParam<T>(IDictionary<string, object>? param, string key) =>
        param is null || param.Count == 0
            ? default
            : (T?)param.TryGetKeyValue<string, object>(key);

    protected async Task SaveCacheChangesAsync(TDbCtx dbContext, ICollection<ChangeInfo<TKey, TValue>> changes)
    {
        await CheckWhichAlreadyExistsAsync(dbContext, changes);

        var set = DbSetAccessor(dbContext);

        foreach (var entityInfo in changes)
        {
            if (entityInfo.ToDelete)
            {
                set.Remove(entityInfo.Value);
            }
            else if (entityInfo.Reason is ChangeReason.Refresh or ChangeReason.Update or ChangeReason.Add)
            {
                if (entityInfo.ExistsInDb)
                    set.Update(entityInfo.Value);
                else
                    await set.AddAsync(entityInfo.Value);
            }
        }
    }

    protected Expression<Func<TValue, bool>> HasKey(TKey key) =>
        EFCoreHelper.HasKey<TKey, TValue>(key, KeyPropertyName);

    protected async Task<(TValue entity, bool existsInDb)> TryFindEntityAsync(TDbCtx dbContext, TValue e) =>
        (e, await ExistsInDbAsync(dbContext, KeyRetriver(e)));

    protected async Task<(TKey key, bool existsInDb)> TryFindEntityAsync(TDbCtx dbContext, TKey key) =>
        (key, await ExistsInDbAsync(dbContext, key));

    protected async Task<bool> ExistsInDbAsync(TDbCtx dbContext, TKey key) =>
        await DbSetAccessor(dbContext).AsNoTracking().AnyAsync(HasKey(key));

    protected bool IndexContains(TKey key) => Index.Contains(key);

    protected Expression<Func<TValue, bool>> KeyIsIn(HashSet<TKey> keys)
    {
        var iParam = Expression.Parameter(typeof(TValue));
        var prop = Expression.Property(iParam, KeyPropertyName);
        var method = keys.GetType().GetMethod(nameof(HashSet<>.Contains), [typeof(TKey)]).Guard("method");
        var call = Expression.Call(Expression.Constant(keys), method, prop);

        return Expression.Lambda<Func<TValue, bool>>(call, iParam);
    }

    protected void AddToIndex(TValue v) => AddKeyToIndex(KeyRetriver(v));

    protected void AddKeyToIndex(TKey e) => Index.Add(e);

    protected void AddKeysToIndex(IEnumerable<TKey> keys) => Index.AddRange(keys);

    protected void RemoveFromIndex(TValue v) => RemoveKeyFromIndex(KeyRetriver(v));

    protected void RemoveKeyFromIndex(TKey e) => Index.Remove(e);

    protected void RemoveKeysWhereFromIndex(Predicate<TKey> match) => Index.RemoveWhere(match);

    private static IList<Change<TValue, TKey>> DistinctChanges(IList<Change<TValue, TKey>> changes)
    {
        List<Change<TValue, TKey>> distinctChanges = [];

        for (var i = changes.Count - 1; i > -1; i--)
        {
            var change = changes[i];

            if (distinctChanges.All(x => !x.Key.Equals(change.Key)))
                distinctChanges.Add(change);
        }

        distinctChanges.Reverse();

        return distinctChanges;
    }

    private async Task<TValue?> FindExistingValueOrAddNewAsync(TKey key,
                                                               bool addNew,
                                                               IDictionary<string, object>? param = null)
    {
        var optional = Cache.Lookup(key);

        if (optional.HasValue)
            return optional.Value;

        TValue? value = null;

        if (!HasCachedAll
            && (!UseIndex || Index.Contains(key)))
            value = await _dbSrv.RunTaskInDbContextAsync(ctx => FindExistingAsync(ctx, key),
                $"Failed to find entry matching key {key}");

        var hasRetrievedFromDb = value is not null;

        if (!hasRetrievedFromDb && addNew)
            value = GetNew(key, param);

        if (value is not null)
            AddValue(value);

        return value;
    }

    private void AddValue(TValue value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value), "Null values are not accepted");

        Cache.AddOrUpdate(value);
        OnRetrievedNew(value);
    }

    private async Task HandleCacheAsync(IChangeSet<TValue, TKey> obj)
    {
        var key = Guid.NewGuid().ToString();

        try
        {
            var task = Task.Run(() => HandleCacheChangesAsync(obj));
            CacheTasks.TryAdd(key, task);
            await task;
        }
        catch (Exception ex)
        {
            ex.HandleException(false);
        }
        finally
        {
            CacheTasks.TryRemove(key, out _);
        }
    }

    private async Task<bool> HandleCacheChangesAsync(IChangeSet<TValue, TKey> changeSet)
    {
        await CacheHandlerSemaphore.WaitAsync();

        try
        {
            var changes = changeSet.Select(x => new ChangeInfo<TKey, TValue>(x)).ToList();

            return changes.Count == 0 || await SaveChangesResolvingConflictsAsync(changes);
        }
        catch (Exception ex)
        {
            ex.HandleException(false);
        }
        finally
        {
            CacheHandlerSemaphore.Release();
        }

        return false;
    }

    /// <summary>
    /// Saves <paramref name="changes" /> and updates <see cref="Index" />. On a concurrency conflict the batch is
    /// reloaded against the database and the rest of it is retried:
    /// <list type="bullet">
    /// <item>a row another writer changed is surfaced: its change is logged at error level and not saved;</item>
    /// <item>a row another writer deleted is re-added only while its key is still cached.</item>
    /// </list>
    /// Every retry resolves or rejects at least one conflicting key, so attempts are bounded by the batch size.
    /// </summary>
    /// <returns><c>true</c> if every change was saved; <c>false</c> if any change was rejected.</returns>
    protected async Task<bool> SaveChangesResolvingConflictsAsync(List<ChangeInfo<TKey, TValue>> changes)
    {
        var maxAttempts = changes.Count + 1;
        var allSaved = true;

        for (var attempt = 1; changes.Count > 0; attempt++)
        {
            try
            {
                await OnChangesDetectedAsync(changes);

                break;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var conflictingKeys = new HashSet<TKey>(ex.Entries.Select(e => e.Entity)
                    .OfType<TValue>()
                    .Select(KeyRetriver));

                if (attempt >= maxAttempts || !changes.Exists(c => conflictingKeys.Contains(c.Key)))
                {
                    _logger.Error(ex,
                        $"{TypeName}: concurrency conflict not resolved, changes for keys [{string.Join(", ", changes.Select(c => c.Key))}] were not saved");

                    return false;
                }

                _logger.Warning(ex, $"{TypeName}: concurrency conflict on save attempt {attempt}, reloading the batch");
                List<ChangeInfo<TKey, TValue>> rejected;
                (changes, rejected) = await ReloadConflictingChangesAsync(changes, conflictingKeys);

                foreach (var change in rejected)
                {
                    _logger.Error(
                        $"{TypeName}: another writer changed the row with key {change.Key}, its cached change was not saved");
                }

                allSaved &= rejected.Count == 0;
            }
        }

        if (UseIndex)
            UpdateIndex(changes);

        return allSaved;
    }

    private async Task<(List<ChangeInfo<TKey, TValue>> retry, List<ChangeInfo<TKey, TValue>> rejected)>
        ReloadConflictingChangesAsync(List<ChangeInfo<TKey, TValue>> changes, HashSet<TKey> conflictingKeys)
    {
        var keys = new HashSet<TKey>(changes.Select(c => c.Key));

        var keysInDb = new HashSet<TKey>(await _dbSrv.RunTaskInDbContextAsync(
            ctx => DbSetAccessor(ctx).AsNoTracking().Where(KeyIsIn(keys)).Select(RetriveKey()).ToListAsync(),
            null,
            false,
            false));

        List<ChangeInfo<TKey, TValue>> retry = [];
        List<ChangeInfo<TKey, TValue>> rejected = [];

        foreach (var change in changes)
        {
            var existsInDb = keysInDb.Contains(change.Key);

            // Without an index, the next attempt re-checks existence in the database itself.
            if (UseIndex)
            {
                if (existsInDb)
                    AddKeyToIndex(change.Key);
                else
                    RemoveKeyFromIndex(change.Key);
            }

            if (existsInDb && conflictingKeys.Contains(change.Key))
                rejected.Add(change);
            // A row deleted elsewhere is re-added only while its key is still cached.
            else if (existsInDb || !change.ExistsInDb || Cache.Lookup(change.Key).HasValue)
                retry.Add(change);
        }

        return (retry, rejected);
    }

    private void UpdateIndex(List<ChangeInfo<TKey, TValue>> changes)
    {
        foreach (var e in changes)
        {
            switch (e.Reason)
            {
                case ChangeReason.Remove:
                    if (Index.Contains(e.Key))
                        RemoveKeyFromIndex(e.Key);

                    break;
                case ChangeReason.Refresh:
                case ChangeReason.Update:
                case ChangeReason.Add:
                    if (!Index.Contains(e.Key))
                        AddKeyToIndex(e.Key);

                    break;
            }
        }
    }

    private async Task CheckWhichAlreadyExistsAsync(TDbCtx dbContext, ICollection<ChangeInfo<TKey, TValue>> changeInfos)
    {
        foreach (var e in changeInfos)
        {
            e.ExistsInDb = UseIndex
                ? IndexContains(e.Key)
                : await ExistsInDbAsync(dbContext, e.Key);
        }
    }

    private async Task<TValue?> FindExistingAsync(TDbCtx ctx, TKey key)
    {
        var entity = await DbSetAccessor(ctx).FindAsync(key);

        return entity is not null
            ? await LoadEntityAsync(ctx, entity)
            : null;
    }

    private void RemoveIfMatch(TKey key, TValue value, Func<TKey, TValue, bool> func)
    {
        if (func(key, value))
            Remove(key);
    }

    private async Task CacheAllAsync(TDbCtx db, HashSet<TKey>? keys = null)
    {
        List<TValue> toCache;

        if (keys?.Count > 0)
        {
            toCache = await IncludeInEntity(DbSetAccessor(db).Where(KeyIsIn(keys))).ToListAsync();
            await EnsureKeysIndexAsync(db);
        }
        else
        {
            toCache = await IncludeInEntity(DbSetAccessor(db)).ToListAsync(); //todo reduce load?
            SetIndex([.. toCache.Select(KeyRetriver)]);
        }

        Cache.Edit(x =>
        {
            foreach (var e in toCache)
                x.AddOrUpdate(e);
        });
    }

    private async Task EnsureKeysIndexAsync(TDbCtx ctx)
    {
        var entries = await DbSetAccessor(ctx).AsNoTracking().Select(RetriveKey()).ToListAsync();

        SetIndex(entries);
    }

    private void SetIndex(List<TKey> entries)
    {
        RemoveKeysWhereFromIndex(x => !entries.Contains(x));
        AddKeysToIndex(entries);

        UseIndex = true;
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            Cache?.Dispose();
            _cacheSubscription?.Dispose();
            CacheHandlerSemaphore.Dispose();
        }

        _isDisposed = true;
        base.Dispose(disposing);
    }
    #endregion
}