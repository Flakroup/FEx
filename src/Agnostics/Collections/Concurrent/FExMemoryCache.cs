using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FEx.Agnostics.Collections.Concurrent;

/// <summary>A thread-safe in-memory key and value cache backed by a <see cref="ConcurrentDictionary{TKey, TValue}"/>.</summary>
/// <typeparam name="TKey">The type of the keys.</typeparam>
/// <typeparam name="TValue">The type of the values.</typeparam>
public class FExMemoryCache<TKey, TValue> : IFExMemoryCache<TKey, TValue>, IDictionary<TKey, TValue>, IDictionary
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _cache;

    /// <summary>Initializes an empty cache.</summary>
    public FExMemoryCache()
    {
        _cache = new();
    }

    /// <summary>Adds a value for a key that is absent, or updates the existing value using a factory.</summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="addValue">The value to store when the key is absent.</param>
    /// <param name="updateValueFactory">Computes the new value from the key and its existing value when the key is present.</param>
    /// <returns>The value now stored for <paramref name="key"/>.</returns>
    public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory) =>
        _cache.AddOrUpdate(key, addValue, updateValueFactory);

    #region IDictionaryImplementation
    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get => _cache[key];
        set => _cache[key] = value;
    }

    /// <inheritdoc />
    public object? this[object key]
    {
        get => ((IDictionary)_cache)[key];
        set => ((IDictionary)_cache)[key] = value;
    }

    /// <inheritdoc />
    public bool IsFixedSize => ((IDictionary)_cache).IsFixedSize;

    /// <inheritdoc />
    public bool IsSynchronized => ((ICollection)_cache).IsSynchronized;
    /// <inheritdoc />
    public object SyncRoot => ((ICollection)_cache).SyncRoot;

    /// <inheritdoc />
    public ICollection<TKey> Keys => _cache.Keys;

    ICollection IDictionary.Values => ((IDictionary)_cache).Values;

    ICollection IDictionary.Keys => ((IDictionary)_cache).Keys;

    /// <inheritdoc />
    public ICollection<TValue> Values => _cache.Values;

    bool IDictionary.IsReadOnly => ((IDictionary)_cache).IsReadOnly;

    int ICollection.Count => _cache.Count;

    int ICollection<KeyValuePair<TKey, TValue>>.Count => _cache.Count;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((IDictionary)_cache).IsReadOnly;

    /// <inheritdoc />
    public void CopyTo(Array array, int index) => ((IDictionary)_cache).CopyTo(array, index);

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_cache).Add(item);

    void ICollection<KeyValuePair<TKey, TValue>>.Clear() => _cache.Clear();

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)_cache).Contains(item);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((IDictionary)_cache).CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)_cache).Remove(item);

    /// <inheritdoc />
    public bool Contains(object key) => ((IDictionary)_cache).Contains(key);

    IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)_cache).GetEnumerator();

    /// <inheritdoc />
    public void Remove(object key) => ((IDictionary)_cache).Remove(key);

    /// <inheritdoc />
    public void Add(object key, object? value) => ((IDictionary)_cache).Add(key, value);

    void IDictionary.Clear() => ((IDictionary)_cache).Clear();

    /// <inheritdoc />
    public void Add(TKey key, TValue value) => ((IDictionary<TKey, TValue>)_cache).Add(key, value);

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => _cache.ContainsKey(key);

    /// <inheritdoc />
    public bool Remove(TKey key) => ((IDictionary<TKey, TValue>)_cache).Remove(key);

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value)
    {
        var found = _cache.TryGetValue(key, out var innerValue);
        value = innerValue!;

        return found;
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_cache).GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        ((IEnumerable<KeyValuePair<TKey, TValue>>)_cache).GetEnumerator();
    #endregion
}