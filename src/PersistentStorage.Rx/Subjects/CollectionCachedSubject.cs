using FEx.Agnostics.Abstractions.Extensions;
using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using FEx.PersistentStorage.Abstractions.Extensions;
using System.Linq;

namespace FEx.PersistentStorage.Rx.Subjects;

public abstract class CollectionCachedSubject<T, TCacheable> : CollectionCachedSubjectBase<T, TCacheable>
    where TCacheable : class, ICacheableItem
{
    protected CollectionCachedSubject(ICacheService cacheService, ClearCacheReason clearCacheReason)
        : base(cacheService, clearCacheReason)
    {
    }

    protected override void RetrieveFromCache()
    {
        if (!Value.IsNullOrEmpty())
            return;

        var cachedData = _cacheService.Get<TCacheable>().ToList();

        if (!cachedData.Any())
            return;

        var data = cachedData.ConvertAll(ConvertCachedDataToModel).Where(cached => cached is not null).ToReadOnlyList();

        if (!data.Any())
            return;

        SynchronizedOnNext(data);
    }
}