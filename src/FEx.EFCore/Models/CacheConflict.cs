namespace FEx.EFCore.Models;

/// <summary>
/// A cached change that was not saved because another writer changed the same row. The cached value is left as it
/// was; call <c>SynchronizedDictionary.ReloadAsync</c> to replace it with the database row once the conflict is
/// resolved.
/// </summary>
/// <typeparam name="TKey">The cache key.</typeparam>
/// <typeparam name="TValue">The cached entity type.</typeparam>
public sealed class CacheConflict<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    public TKey Key { get; }

    /// <summary>The cached instance whose change was rejected; it still holds the rejected values.</summary>
    public TValue CachedValue { get; }

    /// <summary>
    /// A detached copy of the row as the other writer left it, or <c>null</c> when the row no longer exists or could
    /// not be read (that failure is logged).
    /// </summary>
    public TValue? DatabaseValue { get; }

    public CacheConflict(TKey key, TValue cachedValue, TValue? databaseValue)
    {
        Key = key;
        CachedValue = cachedValue;
        DatabaseValue = databaseValue;
    }
}
