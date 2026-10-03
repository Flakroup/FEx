using FEx.Agnostics.Abstractions.Interfaces.Collections;
using System.Collections;
using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Collections;

/// <summary>A dictionary view over an existing dictionary that is used as one direction of a <see cref="Map{TForwardKey, TReverseKey}" />.</summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public class Index<TKey, TValue> : IIndex<TKey, TValue>
{
    private readonly IDictionary<TKey, TValue> _dictionary;

    /// <inheritdoc />
    public TValue this[TKey index]
    {
        get => _dictionary[index];
        set => _dictionary[index] = value;
    }

    /// <inheritdoc />
    public int Count => _dictionary.Count;

    /// <inheritdoc />
    public bool IsReadOnly => _dictionary.IsReadOnly;

    /// <inheritdoc />
    ICollection<TValue> IDictionary<TKey, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => _dictionary.Keys;

    /// <summary>Initializes the index over a dictionary.</summary>
    /// <param name="dictionary">The dictionary to wrap.</param>
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

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    /// <inheritdoc />
    public bool Remove(TKey key) => _dictionary.Remove(key);

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value)
    {
        var found = _dictionary.TryGetValue(key, out var v);

        // v is default(TValue) when not found; the bool result guards meaningful reads.
        value = v!;

        return found;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
}