using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using System;

namespace FEx.PersistentStorage.Rx.Subjects;

public abstract class SingleCachedSubject<T, TCacheable> : CachedSubjectBase<T, T, TCacheable>
    where TCacheable : class, ICacheableItem
{
    protected SingleCachedSubject(ICacheService cacheService,
                                  ClearCacheReason clearCacheReason,
                                  T defaultValue = default)
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
        TCacheable cacheableData = ConvertModelToCachedData(value);
        _cacheService.ReplaceWith(cacheableData);

        base.OnNext(value);
    }

    protected override void RetrieveFromCache()
    {
        if (!ValueIsEqualTo(_defaultValue))
            return;

        TCacheable cachedData = _cacheService.FirstOrDefault<TCacheable>();

        if (cachedData is null)
        {
            OnNext(_defaultValue);

            return;
        }

        T data = ConvertCachedDataToModel(cachedData);
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