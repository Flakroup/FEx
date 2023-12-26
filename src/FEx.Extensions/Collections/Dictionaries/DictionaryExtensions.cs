using FEx.Extensions.Collections.Enumerables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Extensions.Collections.Dictionaries;

/// <summary>
///     IDictionary extensions class.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    ///     Adds the range.
    /// </summary>
    /// <typeparam name="TK">The type of the key.</typeparam>
    /// <typeparam name="TV">The type of the element.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="merged">The merged.</param>
    public static void AddRangeToDictionary<TK, TV>(this IDictionary<TK, TV> dictionary,
                                                    IEnumerable<KeyValuePair<TK, TV>> merged)
    {
        var deferredList = merged.Guard(nameof(merged)).ToList();
        var cDic = dictionary as ConcurrentDictionary<TK, TV>;

        if (cDic is not null)
            deferredList.ForEachInEnumerable(pair => cDic.TryAdd(pair.Key, pair.Value));
        else
            deferredList.ForEachInEnumerable(pair => dictionary.Add(pair.Key, pair.Value));
    }

    /// <summary>
    ///     Transforms to the merged dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <typeparam name="TOutKey">The type of the out key.</typeparam>
    /// <typeparam name="TOutElement">The type of the out element.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="valuesSelector">The values selector.</param>
    /// <returns>Merged dictionary.</returns>
    public static IDictionary<TOutKey, IEnumerable<TOutElement>>
        ToMergedDictionary<TKey, TElement, TOutKey, TOutElement>(this IDictionary<TKey, IList<TElement>> source,
                                                                 Func<TKey, TOutKey> keySelector,
                                                                 Func<IEnumerable<TElement>, IEnumerable<TOutElement>>
                                                                     valuesSelector)
    {
        var result = new Dictionary<TOutKey, IEnumerable<TOutElement>>();

        foreach (KeyValuePair<TKey, IList<TElement>> item in source)
        {
            IEnumerable<TOutElement> values;
            IEnumerable<TOutElement> valuesToMerge = valuesSelector(item.Value);
            TOutKey key = keySelector(item.Key);

            values = result.TryGetValue(key, out values)
                ? values.Concat(valuesToMerge)
                : valuesToMerge;

            result[key] = values;
        }

        return result;
    }

    /// <summary>
    ///     Merges the specified dictionaries.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="merged">The merged.</param>
    /// <returns>Merged dictionaries.</returns>
    public static IDictionary<TKey, IEnumerable<TElement>> Merge<TKey, TElement>(
        this IDictionary<TKey, IEnumerable<TElement>> source,
        IDictionary<TKey, IEnumerable<TElement>> merged)
    {
        foreach (KeyValuePair<TKey, IEnumerable<TElement>> pair in merged)
        {
            if (source.TryGetValue(pair.Key, out IEnumerable<TElement> elements))
                source[pair.Key] = elements.Concat(pair.Value);
            else
                source[pair.Key] = pair.Value.ToList();
        }

        return source;
    }

    /// <summary>
    ///     Tries to get key value.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="fallback">The fallback.</param>
    /// <returns>
    ///     TValue
    /// </returns>
    public static TValue TryGetKeyValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
                                                      TKey key,
                                                      TValue fallback = default)
    {
        var cDic = dictionary as ConcurrentDictionary<TKey, TValue>;

        if (cDic is not null)
            return cDic.TryGetValue(key, out TValue value)
                ? value
                : fallback;

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

    /// <summary>
    ///     Returns the value in an IDictionary at the given key, or creates a new value using the given delegate, adds it at
    ///     the given key, and returns the new value.
    /// </summary>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="createValueToAdd">The create value to add.</param>
    /// <returns></returns>
    public static TV GetOrAddValue<TK, TV>(this IDictionary<TK, TV> dictionary, TK key, Func<TV> createValueToAdd)
    {
        var cDic = dictionary as ConcurrentDictionary<TK, TV>;

        if (cDic is not null)
            return cDic.GetOrAdd(key, _ => createValueToAdd());

        if (!dictionary.TryGetValue(key, out TV v))
        {
            v = createValueToAdd();
            dictionary.Add(key, v);

            return dictionary[key];
        }

        return v;
    }

    public static (bool isSuccess, TV value) GetValue<TK, TV>(this IDictionary<TK, TV> dictionary, TK key)
    {
        bool res = dictionary.TryGetValue(key, out TV v);

        return (res, v);
    }

    /// <summary>
    ///     Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key
    ///     does not already exist, or updates a key/value pair in the
    /// <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> by using the specified function.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <param name="key">The key to be added or whose value should be updated</param>
    /// <param name="valueToAddOrUpdate">The function used to generate a new value</param>
    /// <returns>The new value for the key.</returns>
    public static TV AddOrUpdateValue<TK, TV>(this IDictionary<TK, TV> dictionary, TK key, TV valueToAddOrUpdate) =>
        dictionary.AddOrUpdateValue(key, () => valueToAddOrUpdate);

    /// <summary>
    ///     Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key
    ///     does not already exist, or updates a key/value pair in the
    /// <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> by using the specified function.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <param name="key">The key to be added or whose value should be updated</param>
    /// <param name="valueToAddOrUpdate">The function used to generate a new value</param>
    /// <returns>The new value for the key.</returns>
    public static TV AddOrUpdateValue<TK, TV>(this IDictionary<TK, TV> dictionary, TK key, Func<TV> valueToAddOrUpdate)
    {
        var cDic = dictionary as ConcurrentDictionary<TK, TV>;

        if (cDic is not null)
            return cDic.AddOrUpdate(key, _ => valueToAddOrUpdate(), (_, _) => valueToAddOrUpdate());

        if (dictionary.ContainsKey(key))
            dictionary[key] = valueToAddOrUpdate();
        else
            dictionary.Add(key, valueToAddOrUpdate());

        return dictionary[key];
    }

    /// <summary>
    ///     Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key
    ///     does not already exist, or updates a key/value pair in the
    /// <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> by using the specified function.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <param name="key">The key to be added or whose value should be updated</param>
    /// <param name="valueToAddOrUpdate">The function used to generate a new value</param>
    /// <returns>The (true, old value) tuple for the key if it was present, else (false, new value).</returns>
    public static (bool hasBeenReplaced, TV removedValue, TV newValue) AddOrReplaceValue<TK, TV>(
        this IDictionary<TK, TV> dictionary,
        TK key,
        Func<TV> valueToAddOrUpdate)
    {
        (bool hadValue, TV oldValue) = dictionary.GetValue(key);
        TV added = dictionary.AddOrUpdateValue(key, valueToAddOrUpdate);

        return (hadValue, oldValue, added);
    }

    public static bool TryAddValue<TK, TV>(this IDictionary<TK, TV> dictionary, TK key, Func<TV> createValueToAdd)
    {
        if (!dictionary.ContainsKey(key))
        {
            TV v = createValueToAdd();
            dictionary.Add(key, v);

            return true;
        }

        return false;
    }

    public static (bool hasBeenRemoved, TV removedValue) RemoveValue<TK, TV>(
        this IDictionary<TK, TV> dictionary,
        TK key)
    {
        TV v;

        bool hasBeenRemoved;
        var cDic = dictionary as ConcurrentDictionary<TK, TV>;

        if (cDic is not null)
        {
            hasBeenRemoved = cDic.TryRemove(key, out v);
        }
        else
        {
            dictionary.TryGetValue(key, out v);
            hasBeenRemoved = dictionary.Remove(key);
        }

        return (hasBeenRemoved, v);
    }

    public static bool ReplaceAndDisposeOldValue<TK, TV>(this IDictionary<TK, TV> dictionary, TK key, Func<TV> func)
        where TV : IDisposable
    {
        (bool hasBeenReplaced, TV removedValue, TV _) = dictionary.AddOrReplaceValue(key, func);

        if (hasBeenReplaced)
            removedValue?.Dispose();

        return hasBeenReplaced;
    }

    public static (bool anyItemHasMatched, IDictionary<TK, TV> removedEntries) RemoveFromDictionaryWhere<TK, TV>(
        this IDictionary<TK, TV> dictionary,
        Func<TK, TV, bool> predicate)
    {
        var anyItemHasMatched = false;
        Dictionary<TK, TV> removedEntries = null;

        for (int i = dictionary.Keys.Count - 1; i > -1; i--)
        {
            TK key = dictionary.Keys.ElementAt(i);

            if (predicate(key, dictionary[key]))
            {
                if (!anyItemHasMatched)
                {
                    anyItemHasMatched = true;
                    removedEntries = [];
                }

                (bool hasBeenRemoved, TV removedValue) = dictionary.RemoveValue(key);

                if (hasBeenRemoved)
                    removedEntries.Add(key, removedValue);
            }
        }

        return (anyItemHasMatched, removedEntries);
    }

    public static void SyncWith<TKey, TValue>(this IDictionary<TKey, TValue> sourceDictionary,
                                              IDictionary<TKey, TValue> syncedDictionary)
    {
        if (sourceDictionary.Count > 0)
            sourceDictionary.RemoveFromDictionaryWhere((key, _) => !syncedDictionary.ContainsKey(key));

        if (sourceDictionary.Count == 0)
            sourceDictionary.AddRangeToDictionary(syncedDictionary);
        else
            foreach (TKey key in syncedDictionary.Keys)
                sourceDictionary.AddOrUpdateValue(key, () => syncedDictionary[key]);
    }

    public static bool IsNotNullOrEmptyDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) =>
        dictionary?.Count > 0;

    public static bool IsNullOrEmptyDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) =>
        dictionary is null || dictionary.Count == 0;
}