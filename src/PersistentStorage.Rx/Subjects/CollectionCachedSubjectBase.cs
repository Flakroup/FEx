using FEx.Agnostics.Abstractions.Extensions;
using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.PersistentStorage.Rx.Subjects;

public abstract class
    CollectionCachedSubjectBase<T, TCacheable> : CachedSubjectBase<IReadOnlyCollection<T>, T, TCacheable>
    where TCacheable : class, ICacheableItem
{
    protected CollectionCachedSubjectBase(ICacheService cacheService, ClearCacheReason clearCacheReason)
        : base(cacheService, clearCacheReason, new List<T>().AsReadOnly())
    {
    }

    public override void OnNext(IReadOnlyCollection<T> value)
    {
        if (value is not null
            && Value.Count == 0
            && value.Count == 0)
            return;

        if (value.IsNullOrEmpty())
        {
            ClearCache();

            return;
        }

        DisposeCurrentData();
        IEnumerable<TCacheable> cacheableData = value!.Select(ConvertModelToCachedData);
        _cacheService.ReplaceWith(cacheableData);

        base.OnNext(value);
    }

    protected override void DisposeCurrentData()
    {
        if (Value.IsNullOrEmpty())
            return;

        foreach (IDisposable disposable in Value.OfType<IDisposable>())
            disposable.Dispose();
    }
}