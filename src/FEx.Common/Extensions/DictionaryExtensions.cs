using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FEx.Common.Extensions;

public static class DictionaryExtensions
{
    /// <summary>
    /// Tries to get key value.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="fallback">The fallback.</param>
    /// <returns>
    /// TValue
    /// </returns>
    public static TValue TryGetKeyValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
                                                      TKey key,
                                                      TValue fallback = default)
    {
        if (dictionary is ConcurrentDictionary<TKey, TValue> cDic)
#if NETSTANDARD
            return cDic.TryGetValue(key, out TValue value)
                ? value
                : fallback;
#else
            return cDic.GetValueOrDefault(key, fallback);
#endif

        if (key is not null
            && dictionary.IsNotNullOrEmptyCollection()
            && dictionary.ContainsKey(key))
        {
            (bool isSuccess, TValue value) = dictionary.GetValue(key);

            if (isSuccess)
                return value;
        }

        return fallback;
    }

    public static (bool isSuccess, TV value) GetValue<TK, TV>(this IDictionary<TK, TV> dictionary, TK key)
    {
        bool res = dictionary.TryGetValue(key, out TV v);

        return (res, v);
    }
}