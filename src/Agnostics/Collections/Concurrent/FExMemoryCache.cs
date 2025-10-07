using FEx.Abstractions.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FEx.Basics.Collections.Concurrent;

public class FExMemoryCache<TKey, TValue> : IFExMemoryCache<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, TValue> _cache;

    public FExMemoryCache()
    {
        _cache = new();
    }

    public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory) =>
        _cache.AddOrUpdate(key, addValue, updateValueFactory);

    #region IDictionaryImplementation
    public TValue this[TKey key]
    {
        get => _cache[key];
        set => _cache[key] = value;
    }

    public object this[object key]
    {
        get => ((IDictionary)_cache)[key];
        set => ((IDictionary)_cache)[key] = value;
    }

    public bool IsFixedSize => ((IDictionary)_cache).IsFixedSize;

    public bool IsSynchronized => ((ICollection)_cache).IsSynchronized;
    public object SyncRoot => ((ICollection)_cache).SyncRoot;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => _cache.Keys;

    ICollection IDictionary.Values => ((IDictionary)_cache).Values;

    ICollection IDictionary.Keys => ((IDictionary)_cache).Keys;

    ICollection<TValue> IDictionary<TKey, TValue>.Values => _cache.Values;

    bool IDictionary.IsReadOnly => ((IDictionary)_cache).IsReadOnly;

    int ICollection.Count => _cache.Count;

    int ICollection<KeyValuePair<TKey, TValue>>.Count => _cache.Count;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((IDictionary)_cache).IsReadOnly;

    public void CopyTo(Array array, int index) => ((IDictionary)_cache).CopyTo(array, index);

    public void Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_cache).Add(item);

    void ICollection<KeyValuePair<TKey, TValue>>.Clear() => _cache.Clear();

    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)_cache).Contains(item);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((IDictionary)_cache).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)_cache).Remove(item);

    public bool Contains(object key) => ((IDictionary)_cache).Contains(key);

    IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)_cache).GetEnumerator();

    public void Remove(object key) => ((IDictionary)_cache).Remove(key);

    public void Add(object key, object value) => ((IDictionary)_cache).Add(key, value);

    void IDictionary.Clear() => ((IDictionary)_cache).Clear();

    public void Add(TKey key, TValue value) => ((IDictionary<TKey, TValue>)_cache).Add(key, value);

    bool IDictionary<TKey, TValue>.ContainsKey(TKey key) => _cache.ContainsKey(key);

    public bool Remove(TKey key) => ((IDictionary<TKey, TValue>)_cache).Remove(key);

    public bool TryGetValue(TKey key, out TValue value) => _cache.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_cache).GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        ((IEnumerable<KeyValuePair<TKey, TValue>>)_cache).GetEnumerator();
    #endregion
}