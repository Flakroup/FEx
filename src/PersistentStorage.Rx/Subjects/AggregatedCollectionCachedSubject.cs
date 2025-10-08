using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using System.Collections.Generic;
using System.Linq;

namespace FEx.PersistentStorage.Rx.Subjects;

public abstract class AggregatedCollectionCachedSubject<T, TCacheable> : CollectionCachedSubject<T, TCacheable>
    where TCacheable : class, ICacheableItem
{
    protected AggregatedCollectionCachedSubject(ICacheService cacheService, ClearCacheReason clearCacheReason)
        : base(cacheService, clearCacheReason)
    {
    }

    public virtual void OnReset(IEnumerable<T> newCollection)
    {
        var newValue = newCollection.ToList();

        DisposeCurrentData();
        IEnumerable<TCacheable> cacheableData = newValue.Select(ConvertModelToCachedData);
        _cacheService.ReplaceWith(cacheableData);

        SynchronizedOnNext(newValue);
    }

    public override void OnNext(IReadOnlyCollection<T> value)
    {
        var newValue = Value.Concat(value).ToList();

        _cacheService.Add(value.Select(ConvertModelToCachedData));

        SynchronizedOnNext(newValue);
    }
}