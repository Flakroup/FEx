using System.Collections.Generic;

namespace FEx.Extensions.Collections.Dictionaries;

/// <summary>
///     IDictionary extensions class.
/// </summary>
public static class ReadOnlyDictionaryExtensions
{
    public static TValue TryGetReadOnlyKeyValue<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue fallback = default)
    {
        if (key != null
            && dictionary.IsNotNullOrEmptyReadOnlyCollection()
            && dictionary.ContainsKey(key))
        {
            (bool isSuccess, TValue value) = dictionary.GetReadOnlyValue(key);

            if (isSuccess)
                return value;
        }

        return fallback;
    }

    public static (bool isSuccess, TV value) GetReadOnlyValue<TK, TV>(this IReadOnlyDictionary<TK, TV> dictionary, TK key)
    {
        bool res = dictionary.TryGetValue(key, out TV v);

        return (res, v);
    }
}