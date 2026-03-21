using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using FEx.PersistentStorage.Abstractions.Extensions;
using System;

namespace FEx.PersistentStorage.Rx.Subjects;

public abstract class SingleCachedSubject<T, TCacheable> : CachedSubjectBase<T, T, TCacheable>
    where TCacheable : class, ICacheableItem
{
    protected SingleCachedSubject(ICacheService cacheService,
                                  ClearCacheReason clearCacheReason)
        : this(cacheService, clearCacheReason, default)
    {
    }

    protected SingleCachedSubject(ICacheService cacheService,
                                  ClearCacheReason clearCacheReason,
                                  T defaultValue)
        : base(cacheService, clearCacheReason, defaultValue)
    {
    }

    public override void OnNext(T value)
    {
        if (ValueIsEqualTo(value))
            return;

        if (value is null)
        {
            ClearCache();

            return;
        }

        DisposeCurrentData();
        var cacheableData = ConvertModelToCachedData(value);
        _cacheService.ReplaceWith(cacheableData);

        base.OnNext(value);
    }

    protected override void RetrieveFromCache()
    {
        if (!ValueIsEqualTo(_defaultValue))
            return;

        var cachedData = _cacheService.FirstOrDefault<TCacheable>();

        if (cachedData is null)
        {
            OnNext(_defaultValue);

            return;
        }

        var data = ConvertCachedDataToModel(cachedData);
        SynchronizedOnNext(data);
    }

    protected override void DisposeCurrentData()
    {
        if (Value is null)
            return;

        if (Value is IDisposable disposable)
            disposable.Dispose();
    }
}