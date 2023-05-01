using System.Collections;
using System.Collections.Generic;

namespace FEx.Utilities.Collections;

public class Index<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    private readonly IDictionary<TKey, TValue> _dictionary;

    public TValue this[TKey index]
    {
        get => _dictionary[index];
        set => _dictionary[index] = value;
    }

    public IEnumerable<TKey> Keys => _dictionary.Keys;
    public IEnumerable<TValue> Values => _dictionary.Values;

    public int Count => _dictionary.Count;

    public Index(IDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _dictionary.GetEnumerator();
    }

    public bool ContainsKey(TKey key)
    {
        return _dictionary.ContainsKey(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return _dictionary.TryGetValue(key, out value);
    }
}