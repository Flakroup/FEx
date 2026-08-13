using FEx.Core.Abstractions.Subjects;
using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using FEx.PersistentStorage.Abstractions.Extensions;
using StrongInject;

namespace FEx.PersistentStorage.Rx.Subjects;

public abstract class CachedSubjectBase<TData, T, TCacheable> : FExBehaviorSubject<TData>, IClearCache,
    IRequiresInitialization where TCacheable : class, ICacheableItem
{
    protected readonly ICacheService _cacheService;

    public ClearCacheReason ClearCacheReason { get; }

#if NETSTANDARD2_0
    /// <inheritdoc />
    public ClearCachePriority ClearCachePriority { get; }
#endif

    protected CachedSubjectBase(ICacheService cacheService, ClearCacheReason clearCacheReason)
        // default! mirrors the FExBehaviorSubject default-value idiom; TData is unconstrained so default may be null and that is the intended empty state.
        : this(cacheService, clearCacheReason, default!)
    {
    }

    protected CachedSubjectBase(ICacheService cacheService, ClearCacheReason clearCacheReason, TData defaultValue)
        : base(defaultValue)
    {
        _cacheService = cacheService;
        ClearCacheReason = clearCacheReason;
    }

    public void ClearCache()
    {
        if (ValueIsEqualTo(_defaultValue))
            return;

        DisposeCurrentData();
        _cacheService.Delete<TCacheable>();

        SynchronizedOnNext(_defaultValue);
    }

    public virtual void Initialize()
    {
        RetrieveFromCache();
    }

    protected abstract void RetrieveFromCache();
    protected abstract void DisposeCurrentData();
    protected abstract TCacheable ConvertModelToCachedData(T data);
    protected abstract T ConvertCachedDataToModel(TCacheable cachedData);
}