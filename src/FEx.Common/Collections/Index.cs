using FEx.Common.Abstractions.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace FEx.Common.Collections;

public class Index<TKey, TValue> : IIndex<TKey, TValue>
{
    private readonly IDictionary<TKey, TValue> _dictionary;

    public TValue this[TKey index]
    {
        get => _dictionary[index];
        set => _dictionary[index] = value;
    }

    public int Count => _dictionary.Count;

    /// <inheritdoc />
    public bool IsReadOnly => _dictionary.IsReadOnly;

    /// <inheritdoc />
    ICollection<TValue> IDictionary<TKey, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => _dictionary.Keys;

    public Index(IDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary;
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item) => _dictionary.Add(item);

    /// <inheritdoc />
    public void Clear() => _dictionary.Clear();

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => _dictionary.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) => _dictionary.Remove(item);

    /// <inheritdoc />
    public void Add(TKey key, TValue value) => _dictionary.Add(key, value);

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    /// <inheritdoc />
    public bool Remove(TKey key) => _dictionary.Remove(key);

    public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
}