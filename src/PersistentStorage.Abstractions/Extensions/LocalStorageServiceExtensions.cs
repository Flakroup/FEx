using System.Collections.Generic;

namespace FEx.PersistentStorage.Abstractions.Extensions;

public static class LocalStorageServiceExtensions
{
    public static IReadOnlyList<T> GetAll<T>(this ILocalStorageService service) where T : ICacheableItem =>
        service.GetAll<T>(null);

    public static T? FirstOrDefault<T>(this ILocalStorageService service) where T : ICacheableItem =>
        service.FirstOrDefault<T>(null);

    public static bool Upsert<T>(this ILocalStorageService service, T item) where T : ICacheableItem =>
        service.Upsert(item, null);

    public static void Upsert<T>(this ILocalStorageService service, IEnumerable<T> items) where T : ICacheableItem =>
        service.Upsert(items, null);

    public static bool DeleteAll<T>(this ILocalStorageService service) where T : ICacheableItem =>
        service.DeleteAll<T>(null);
}