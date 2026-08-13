using FEx.Agnostics.Abstractions.Extensions;
using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using FEx.PersistentStorage.Abstractions.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace FEx.PersistentStorage.Rx.Subjects;

public abstract class
    EnhancedCollectionCachedSubject<T, TCacheable, TEnhancement> : CollectionCachedSubjectBase<T, TCacheable>
    where TCacheable : class, ICacheableItem
{
    protected EnhancedCollectionCachedSubject(ICacheService cacheService, ClearCacheReason clearCacheReason)
        : base(cacheService, clearCacheReason)
    {
    }

    protected abstract TEnhancement GetEnhancement(List<TCacheable> cachedData);
    protected abstract T ConvertCachedDataToModel(TCacheable cachedData, TEnhancement enhancement);

    protected override void RetrieveFromCache()
    {
        if (!Value.IsNullOrEmpty())
            return;

        var cachedData = _cacheService.Get<TCacheable>().ToList();

        if (!cachedData.Any())
            return;

        var enhancement = GetEnhancement(cachedData);
        var data = cachedData.ConvertAll(c => ConvertCachedDataToModel(c, enhancement));

        SynchronizedOnNext(data);
    }
}