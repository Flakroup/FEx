using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Extensions.Collections.Dictionaries;

/// <summary>
/// IReadOnlyDictionary extensions class.
/// </summary>
public static class ReadOnlyDictionaryExtensions
{
    /// <summary>Gets the value for a key, or a fallback when the key is missing.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dictionary">The dictionary to read.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="fallback">The value returned when the key is not found.</param>
    /// <returns>The value for the key, or <paramref name="fallback" />.</returns>
    public static TValue TryGetReadOnlyKeyValue<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary,
                                                              TKey key,
                                                              TValue fallback = default!)
    {
        if (key is not null
            && dictionary.IsNotNullOrEmptyReadOnlyCollection()
            && dictionary.ContainsKey(key))
        {
            var (isSuccess, value) = dictionary.GetReadOnlyValue(key);

            if (isSuccess)
                return value;
        }

        return fallback;
    }

    /// <summary>Tries to get a value by key.</summary>
    /// <typeparam name="TK">The key type.</typeparam>
    /// <typeparam name="TV">The value type.</typeparam>
    /// <param name="dictionary">The dictionary to read.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>A tuple of whether the key was found and the value (the default when not found).</returns>
    public static (bool isSuccess, TV value) GetReadOnlyValue<TK, TV>(this IReadOnlyDictionary<TK, TV> dictionary,
                                                                      TK key)
    {
        var res = dictionary.TryGetValue(key, out var v);

        // v is meaningful only when res is true; otherwise it is default(TV) by the Try pattern.
        return (res, v!);
    }
}