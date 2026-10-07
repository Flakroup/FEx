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
using System.Reactive.Disposables;
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
    // The instance ReloadAsync is putting into (or taking out of) the cache on this thread; SourceCache publishes the
    // edit before it returns, so the subscription sees that change here and drops it instead of saving it.
    [ThreadStatic]
    private static TValue? _reloadTarget;

    // Sequence numbers of the change sets published to the current save pipeline and not yet saved or dropped.
    // ReloadAsync waits until none is left at or below the last number published when it started. Replacing the
    // pipeline starts a new generation and clears the set, since Rx disposes the old one asynchronously and its
    // batches may run, or not, after that.
    private readonly object _pendingLock = new();
    private readonly SortedSet<long> _pendingChanges = [];
    private readonly List<PendingWaiter> _pendingWaiters = [];
    private long _lastChange;
    private int _generation;
    private bool _isDisposed;
    private string[] _observedProperties;
    private IDisposable? _cacheSubscription;
    private Func<TValue, TKey>? _keyRetriver;

    public ConcurrentHashSet<TKey> Index { get; }
    public bool UseIndex { get; protected set; }

    public int Count => Cache.Count;

    /// <summary>
    /// Raised once per cached change that was not saved because another writer changed the same row. An update is
    /// reported while the rejected instance is still the cached value for its key (not after it was reloaded or
    /// removed); a removal (<see cref="CacheConflict{TKey, TValue}.IsRemoval" />) while the key is still not cached. The
    /// rejected value keeps its stale concurrency token, so later edits of the key are rejected too until
    /// <see cref="ReloadAsync" /> replaces it.
    /// <para>
    /// Raised after the batch save has finished, outside the save lock and after the save left
    /// <see cref="CacheTasks" />, so a handler may wait on the cache (call <see cref="ReloadAsync" />, say). For the
    /// buffered save pipeline that is a thread-pool thread; for a direct
    /// <see cref="SaveChangesResolvingConflictsAsync" /> call it is the caller's continuation. An exception from a
    /// handler is logged and does not affect the save or the other handlers.
    /// </para>
    /// </summary>
    public event EventHandler<CacheConflict<TKey, TValue>>? ConflictDetected;

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
        : base([dbService])
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

    /// <summary>
    /// Replaces the cached value of <paramref name="key" /> with the row as it is in the database, loaded with the
    /// dictionary's normal query (<see cref="IncludeInEntity" /> and <see cref="LoadEntityAsync" />), or removes the
    /// entry when the row no longer exists. Cache changes already published to the current save pipeline (buffered or
    /// being saved) are saved first; changes a pipeline replaced by <c>ResetAsync</c> + <c>InitializeAsync</c> still held
    /// may be dropped or saved late, and are not waited for. The replacement is not saved back. Call it when a
    /// <see cref="ConflictDetected" /> conflict is resolved, from the thread that owns the cached values.
    /// </summary>
    /// <returns>The new cached value, or <c>null</c> when the row no longer exists.</returns>
    /// <exception cref="ObjectDisposedException">The dictionary was disposed before the pending changes were saved.</exception>
    public async Task<TValue?> ReloadAsync(TKey key, CancellationToken cancellationToken = default)
    {
        // An edit still buffered or being saved is written first, so the row read below includes it and the cache
        // matches the database afterwards.
        await WaitForPendingChangesAsync(cancellationToken);

        var fresh = await _dbSrv.RunTaskInDbContextAsync(async ctx =>
            {
                var entity = await IncludeInEntity(DbSetAccessor(ctx).Where(HasKey(key)))
                    .FirstOrDefaultAsync(cancellationToken);

                return entity is null ? null : await LoadEntityAsync(ctx, entity);
            },
            $"Failed to reload entry matching key {key}",
            false,
            false);

        var current = Cache.Lookup(key);
        _reloadTarget = fresh ?? (current.HasValue ? current.Value : null);

        try
        {
            if (fresh is not null)
                Cache.AddOrUpdate(fresh);
            else if (current.HasValue)
                Cache.Remove(key);
        }
        finally
        {
            _reloadTarget = null;
        }

        // Outside the marker: edits the hook makes to the new value are saved, as for any other retrieved value.
        if (fresh is not null)
            OnRetrievedNew(fresh);

        if (UseIndex)
        {
            if (fresh is null)
                RemoveKeyFromIndex(key);
            else
                AddKeyToIndex(key);
        }

        return fresh;
    }

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
        var mappedProperties = _dbSrv.Mappings[typeof(TValue).FullName.Guard(nameof(TValue))].Properties;

        _observedProperties = _observedProperties is null
            ? [.. mappedProperties]
            : [.. _observedProperties, .. mappedProperties];

        var cacheObservable =
            Cache.Connect().AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(_observedProperties));

        if (_observables?.Any() == true)
            cacheObservable =
                _observables.Aggregate(cacheObservable, (current, o) => current.AutoRefreshOnObservable(o));

        if (ReplacedSubscription is { } keep)
            keep(_cacheSubscription);
        else
            _cacheSubscription?.Dispose();

        var generation = StartGeneration();
        var subscribed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // SubscribeTask subscribes on the thread pool; signal once the cache is listened to, so an edit made after
        // InitializeAsync returns reaches this subscription.
        var source = Observable.Create<IChangeSet<TValue, TKey>>(observer =>
        {
            var listen = ListenAsync(observer);
            LastSubscribe = listen.ContinueWith(static _ => { }, TaskScheduler.Default);

            return listen;
        });

        var pipelineSubscription = source.Select(WithoutReloads)
            .Where(changeSet => changeSet.Count > 0)
            .Select(changeSet => (Sequence: Publish(generation), ChangeSet: changeSet))
            // The replaced subscription, until its asynchronous disposal, sees edits the new one saves: drop them.
            .Where(c => c.Sequence != 0)
            .Buffer(TimeSpan.FromMilliseconds(100))
            .Where(x => x.Count > 0)
            .Select(x =>
            {
                List<long> sequences = [.. x.Select(c => c.Sequence)];
                BatchSelected?.Invoke();

                var changes = x.SelectMany(c => c.ChangeSet)
                    .Where(c => c.Reason is not ChangeReason.Moved
                                && (c.Reason != ChangeReason.Add || !Index.Contains(c.Key)))
                    .ToList();

                return (Sequences: sequences,
                    ChangeSet: changes.Count == 0 ? null : new ChangeSet<TValue, TKey>(DistinctChanges(changes)));
            })
            .SubscribeTask((batch, _) => HandleBatchAsync(batch.Sequences, batch.ChangeSet));

        if (BeforePipelineAssigned is { } beforeAssigned)
            await beforeAssigned().ConfigureAwait(false);

        // Disposed before SubscribeOn ran the subscribe (Rx then skips it): fail InitializeAsync instead of hanging.
        var subscription = new CompositeDisposable(pipelineSubscription,
            Disposable.Create(() => subscribed.TrySetException(new ObjectDisposedException(TypeName))));
        bool disposed;

        // With Dispose, which sets _isDisposed and then reads _cacheSubscription under the same lock: either Dispose
        // disposes this subscription, or this sees _isDisposed and disposes it.
        lock (_pendingLock)
        {
            _cacheSubscription = subscription;
            disposed = _isDisposed;
        }

        if (disposed)
        {
            subscription.Dispose();
            // Disposing faulted the signal; observe it, the exception thrown below is the one reported.
            _ = subscribed.Task.Exception;

            throw new ObjectDisposedException(TypeName);
        }

        await subscribed.Task;

        async Task<IDisposable> ListenAsync(IObserver<IChangeSet<TValue, TKey>> observer)
        {
            if (BeforeSubscribe is { } hold)
                await hold().ConfigureAwait(false);

            // Disposed already: InitializeAsync has failed, do not listen to a cache that may be disposed too.
            if (subscribed.Task.IsCompleted)
            {
                SubscribeDecided?.Invoke(false);

                return Disposable.Empty;
            }

            SubscribeDecided?.Invoke(true);

            try
            {
                return cacheObservable.Subscribe(observer);
            }
            finally
            {
                subscribed.TrySetResult(true);
            }
        }
    }

    protected TKey KeyRetriver(TValue value) => (_keyRetriver ??= RetriveKey().Compile()).Invoke(value);

    protected T? GetParam<T>(IDictionary<string, object>? param, string key) =>
        param is null || param.Count == 0
            ? default
            : (T?)param.TryGetKeyValue<string, object>(key);

    /// <summary>
    /// Stages <paramref name="changes" /> on <paramref name="dbContext" />; the caller saves it. A changed value whose row
    /// exists is loaded (with its owned types) and the cached state is applied onto it, so only the value's own columns,
    /// its owned types and the foreign keys of its reference navigations are written, never a related entity; the cached
    /// concurrency tokens are the original values, so a row changed elsewhere is a conflict. A new value is added and a
    /// removed one removed with its graph, as <c>DbSet.Add</c> and <c>DbSet.Remove</c> do, except that an entity of the
    /// graph whose row is loaded for this save is not attached (the loaded row stands for it). With
    /// <see cref="UseIndex" />, a row the index lists but another writer deleted is re-added only while its key is still
    /// cached; otherwise its change is dropped from <paramref name="changes" />.
    /// </summary>
    protected async Task SaveCacheChangesAsync(TDbCtx dbContext, ICollection<ChangeInfo<TKey, TValue>> changes)
    {
        await CheckWhichAlreadyExistsAsync(dbContext, changes);

        var set = DbSetAccessor(dbContext);
        var saves = changes.Where(c => IsSave(c.Reason) && c.ExistsInDb).ToList();
        var rows = await LoadRowsAsync(set, saves);
        List<ChangeInfo<TKey, TValue>> dropped = [];

        // Before any graph is attached below, so a loaded row is never replaced by a cached instance of the same key.
        foreach (var change in saves)
        {
            if (rows.TryGetValue(change.Key, out var row))
                CachedValueApplier.Apply(dbContext, dbContext.Entry(row), change.Value);
            else if (UseIndex && !Cache.Lookup(change.Key).HasValue)
                dropped.Add(change);
            else
                change.ExistsInDb = false;
        }

        foreach (var change in dropped)
        {
            RemoveKeyFromIndex(change.Key);
            changes.Remove(change);
        }

        var tracked = CachedValueApplier.TrackedKeys(dbContext);

        foreach (var entityInfo in changes)
        {
            if (entityInfo.ToDelete)
                CachedValueApplier.AttachGraph(dbContext, entityInfo.Value, true, tracked);
            else if (IsSave(entityInfo.Reason) && !entityInfo.ExistsInDb)
                CachedValueApplier.AttachGraph(dbContext, entityInfo.Value, false, tracked);
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

    private async Task HandleBatchAsync(List<long> sequences, IChangeSet<TValue, TKey>? changeSet)
    {
        var conflicts = new List<(ChangeInfo<TKey, TValue> Change, TValue? DatabaseValue)>();

        try
        {
            if (changeSet is not null)
                await HandleCacheAsync(changeSet, conflicts);
        }
        finally
        {
            ReleaseChanges(sequences);
        }

        // After the save released CacheHandlerSemaphore and left CacheTasks, so a handler may wait on the cache.
        RaiseConflicts(conflicts);
    }

    private async Task HandleCacheAsync(IChangeSet<TValue, TKey> obj,
                                        List<(ChangeInfo<TKey, TValue> Change, TValue? DatabaseValue)> conflicts)
    {
        var key = Guid.NewGuid().ToString();

        try
        {
            var task = Task.Run(() => HandleCacheChangesAsync(obj, conflicts));
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

    private async Task<bool> HandleCacheChangesAsync(IChangeSet<TValue, TKey> changeSet,
                                                     List<(ChangeInfo<TKey, TValue> Change, TValue? DatabaseValue)>
                                                         conflicts)
    {
        await CacheHandlerSemaphore.WaitAsync();

        try
        {
            var changes = changeSet.Select(x => new ChangeInfo<TKey, TValue>(x)).ToList();

            return changes.Count == 0 || await SaveChangesCollectingConflictsAsync(changes, conflicts);
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
    /// <item>
    /// a row another writer changed is surfaced: its change is logged at error level, not saved, and raised through
    /// <see cref="ConflictDetected" />; the cached value is left as it is until <see cref="ReloadAsync" />;
    /// </item>
    /// <item>a row another writer deleted is re-added only while its key is still cached.</item>
    /// </list>
    /// Every retry resolves or rejects at least one conflicting key, so attempts are bounded by the batch size.
    /// </summary>
    /// <returns><c>true</c> if every change was saved; <c>false</c> if any change was rejected.</returns>
    protected async Task<bool> SaveChangesResolvingConflictsAsync(List<ChangeInfo<TKey, TValue>> changes)
    {
        var conflicts = new List<(ChangeInfo<TKey, TValue> Change, TValue? DatabaseValue)>();

        try
        {
            return await SaveChangesCollectingConflictsAsync(changes, conflicts);
        }
        finally
        {
            // Also when a later attempt throws: the changes rejected before it are still reported.
            RaiseConflicts(conflicts);
        }
    }

    // The save itself; rejected changes are collected so the caller raises ConflictDetected once the save is over.
    private async Task<bool> SaveChangesCollectingConflictsAsync(List<ChangeInfo<TKey, TValue>> changes,
                                                                 List<(ChangeInfo<TKey, TValue> Change, TValue?
                                                                     DatabaseValue)> conflicts)
    {
        var maxAttempts = changes.Count + 1;
        var allRejected = new List<ChangeInfo<TKey, TValue>>();

        try
        {
            return await SaveChangesRejectingConflictsAsync(changes, maxAttempts, allRejected);
        }
        finally
        {
            // One read of the database rows for every change rejected in any attempt of this save.
            if (allRejected.Count > 0)
                await CollectConflictsAsync(allRejected, conflicts);
        }
    }

    private async Task<bool> SaveChangesRejectingConflictsAsync(List<ChangeInfo<TKey, TValue>> changes,
                                                                int maxAttempts,
                                                                List<ChangeInfo<TKey, TValue>> allRejected)
    {
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
                allRejected.AddRange(rejected);
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

    private async Task CollectConflictsAsync(List<ChangeInfo<TKey, TValue>> rejected,
                                             List<(ChangeInfo<TKey, TValue> Change, TValue? DatabaseValue)> conflicts)
    {
        if (ConflictDetected is null)
            return;

        var keys = new HashSet<TKey>(rejected.Select(c => c.Key));
        var rows = new Dictionary<TKey, TValue>();

        try
        {
            foreach (var row in await _dbSrv.RunTaskInDbContextAsync(
                         ctx => IncludeInEntity(DbSetAccessor(ctx).AsNoTracking().Where(KeyIsIn(keys))).ToListAsync(),
                         null,
                         false,
                         false))
                rows[KeyRetriver(row)] = row;
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                $"{TypeName}: could not read the database values of rejected changes, keys [{string.Join(", ", keys)}]");
        }

        conflicts.AddRange(rejected.Select(change =>
            (change, rows.TryGetValue(change.Key, out var row) ? row : null)));
    }

    private void RaiseConflicts(List<(ChangeInfo<TKey, TValue> Change, TValue? DatabaseValue)> conflicts)
    {
        foreach (var (change, databaseValue) in conflicts)
        {
            var isRemoval = change.Reason == ChangeReason.Remove;
            var cached = Cache.Lookup(change.Key);

            // An update only while the rejected instance is still what the cache holds for the key, a removal only
            // while the key is still not cached: a value reloaded, removed or re-added since then is not in conflict.
            if (isRemoval ? cached.HasValue : !cached.HasValue || !ReferenceEquals(cached.Value, change.Value))
                continue;

            RaiseConflict(new(change.Key, change.Value, databaseValue, isRemoval));
        }
    }

    /// <summary>Test seam: runs after a batch left the buffer and before it is handled.</summary>
    internal Action? BatchSelected { get; set; }

    /// <summary>
    /// Test seam: awaited by the thread-pool subscribe of the save pipeline before it listens to the cache. A test holds
    /// the subscribe by returning a pending task, which keeps the pool thread free.
    /// </summary>
    internal Func<Task>? BeforeSubscribe { get; set; }

    /// <summary>Test seam: told whether the thread-pool subscribe listens to the cache (<c>false</c> once disposed).</summary>
    internal Action<bool>? SubscribeDecided { get; set; }

    /// <summary>
    /// Test seam: the latest thread-pool subscribe of the save pipeline; once it completes, nothing of the subscribe
    /// is rooted any more.
    /// </summary>
    internal Task? LastSubscribe { get; private set; }

    /// <summary>Test seam: awaited after the save pipeline is built and before it becomes the dictionary's subscription.</summary>
    internal Func<Task>? BeforePipelineAssigned { get; set; }

    /// <summary>Test seam: receives the replaced subscription instead of disposing it, as if its disposal were late.</summary>
    internal Action<IDisposable?>? ReplacedSubscription { get; set; }

    internal int Generation => Volatile.Read(ref _generation);

    internal long LastChange
    {
        get
        {
            lock (_pendingLock)
                return _lastChange;
        }
    }

    internal int PendingChangeCount
    {
        get
        {
            lock (_pendingLock)
                return _pendingChanges.Count;
        }
    }

    internal int PendingWaiterCount
    {
        get
        {
            lock (_pendingLock)
                return _pendingWaiters.Count;
        }
    }

    internal bool IsPending(long sequence)
    {
        lock (_pendingLock)
            return _pendingChanges.Contains(sequence);
    }

    // A change set of a closed generation is not tracked; 0 is never pending.
    internal long Publish(int generation)
    {
        lock (_pendingLock)
        {
            if (generation != _generation || _isDisposed)
                return 0;

            var sequence = ++_lastChange;
            _pendingChanges.Add(sequence);

            return sequence;
        }
    }

    // Closes the current generation, releasing all its numbers (buffered, taken from the buffer or being saved).
    private int StartGeneration()
    {
        List<PendingWaiter> released;
        int generation;

        lock (_pendingLock)
        {
            generation = ++_generation;
            _pendingChanges.Clear();

            released = TakeReleasedWaiters();
        }

        SignalReleased(released);

        return generation;
    }

    // Removing a number twice is harmless, so a batch of a closed generation that still finishes changes nothing.
    private void ReleaseChanges(List<long> sequences)
    {
        List<PendingWaiter> released;

        lock (_pendingLock)
        {
            foreach (var sequence in sequences)
                _pendingChanges.Remove(sequence);

            released = TakeReleasedWaiters();
        }

        SignalReleased(released);
    }

    // Under _pendingLock.
    private List<PendingWaiter> TakeReleasedWaiters()
    {
        var released = _pendingWaiters.FindAll(w => NothingPendingUpTo(w.Target));
        _pendingWaiters.RemoveAll(released.Contains);

        return released;
    }

    private static void SignalReleased(List<PendingWaiter> released)
    {
        foreach (var waiter in released)
            waiter.Signal.TrySetResult(true);
    }

    private void ReleaseWaitersOnDispose()
    {
        List<PendingWaiter> waiters;

        lock (_pendingLock)
        {
            _isDisposed = true;
            _pendingChanges.Clear();
            waiters = [.. _pendingWaiters];
            _pendingWaiters.Clear();
        }

        foreach (var waiter in waiters)
            waiter.Signal.TrySetException(new ObjectDisposedException(TypeName));
    }

    private bool NothingPendingUpTo(long target) => _pendingChanges.Count == 0 || _pendingChanges.Min > target;

    // Waits until every change set published to the save pipeline so far has been saved or dropped.
    private async Task WaitForPendingChangesAsync(CancellationToken cancellationToken)
    {
        PendingWaiter waiter;
        var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_pendingLock)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(TypeName);

            if (NothingPendingUpTo(_lastChange))
                return;

            waiter = new(_lastChange, signal);
            _pendingWaiters.Add(waiter);
        }

        try
        {
            using (cancellationToken.Register(() => signal.TrySetCanceled(cancellationToken)))
                await signal.Task;
        }
        finally
        {
            lock (_pendingLock)
                _pendingWaiters.Remove(waiter);
        }
    }

    // Each handler in isolation: a throwing one must not stop the others or the save loop.
    private void RaiseConflict(CacheConflict<TKey, TValue> conflict)
    {
        if (ConflictDetected is not { } handlers)
            return;

        foreach (var handler in handlers.GetInvocationList().Cast<EventHandler<CacheConflict<TKey, TValue>>>())
        {
            try
            {
                handler(this, conflict);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{TypeName}: a ConflictDetected handler failed for key {conflict.Key}");
            }
        }
    }

    private static IChangeSet<TValue, TKey> WithoutReloads(IChangeSet<TValue, TKey> changeSet)
    {
        var target = _reloadTarget;

        return target is null
            ? changeSet
            : new ChangeSet<TValue, TKey>(changeSet.Where(c => !ReferenceEquals(c.Current, target)));
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

    private static bool IsSave(ChangeReason reason) =>
        reason is ChangeReason.Refresh or ChangeReason.Update or ChangeReason.Add;

    // Without an index only removals are checked here; for a saved value, loading its row is the check.
    private async Task CheckWhichAlreadyExistsAsync(TDbCtx dbContext, ICollection<ChangeInfo<TKey, TValue>> changeInfos)
    {
        if (UseIndex)
        {
            foreach (var e in changeInfos)
                e.ExistsInDb = IndexContains(e.Key);

            return;
        }

        var removals = new HashSet<TKey>(changeInfos.Where(c => c.Reason == ChangeReason.Remove).Select(c => c.Key));

        var keysInDb = removals.Count == 0
            ? []
            : new HashSet<TKey>(await DbSetAccessor(dbContext)
                .AsNoTracking()
                .Where(KeyIsIn(removals))
                .Select(RetriveKey())
                .ToListAsync());

        foreach (var e in changeInfos)
            e.ExistsInDb = IsSave(e.Reason) || keysInDb.Contains(e.Key);
    }

    // One tracking query for every saved value of the batch; owned types are loaded with their owner. Query filters are
    // ignored: whether the row exists is decided by its key, not by a soft-delete or tenant filter.
    private async Task<Dictionary<TKey, TValue>> LoadRowsAsync(DbSet<TValue> set, List<ChangeInfo<TKey, TValue>> saves)
    {
        if (saves.Count == 0)
            return [];

        var keys = new HashSet<TKey>(saves.Select(c => c.Key));

        return (await set.IgnoreQueryFilters().AsTracking().Where(KeyIsIn(keys)).ToListAsync()).ToDictionary(KeyRetriver);
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
            IDisposable? subscription;

            // See OnInitializeAsync: a subscription assigned after this is disposed there.
            lock (_pendingLock)
            {
                _isDisposed = true;
                subscription = _cacheSubscription;
            }

            subscription?.Dispose();
            // A ReloadAsync still waiting for changes the subscription will never save fails instead of hanging.
            ReleaseWaitersOnDispose();
            CacheHandlerSemaphore.Dispose();
        }

        _isDisposed = true;
        base.Dispose(disposing);
    }
    #endregion

    private sealed class PendingWaiter
    {
        public long Target { get; }

        public TaskCompletionSource<bool> Signal { get; }

        public PendingWaiter(long target, TaskCompletionSource<bool> signal)
        {
            Target = target;
            Signal = signal;
        }
    }
}