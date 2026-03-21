using System.Collections.Generic;

namespace FEx.PersistentStorage.Abstractions.Extensions;

public static class CacheServiceExtensions
{
    public static bool Upsert<T>(this ICacheService service, T item) where T : ICacheableItem =>
        service.Upsert(item, null);

    public static void Upsert<T>(this ICacheService service, IEnumerable<T> items) where T : ICacheableItem =>
        service.Upsert(items, null);

    public static bool Delete<T>(this ICacheService service) where T : ICacheableItem =>
        service.Delete<T>((System.Linq.Expressions.Expression<System.Func<T, bool>>)null);

    public static T FirstOrDefault<T>(this ICacheService service) where T : ICacheableItem =>
        service.FirstOrDefault<T>(null);

    public static IReadOnlyCollection<T> Get<T>(this ICacheService service) where T : ICacheableItem =>
        service.Get<T>(null);
}
