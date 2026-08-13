using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Extensions.Collections.Dictionaries;

/// <summary>
/// IReadOnlyDictionary extensions class.
/// </summary>
public static class ReadOnlyDictionaryExtensions
{
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

    public static (bool isSuccess, TV value) GetReadOnlyValue<TK, TV>(this IReadOnlyDictionary<TK, TV> dictionary,
                                                                      TK key)
    {
        var res = dictionary.TryGetValue(key, out var v);

        // v is meaningful only when res is true; otherwise it is default(TV) by the Try pattern.
        return (res, v!);
    }
}