using System;
using System.Collections.Generic;

namespace FEx.Basics.Collections;

public class Map<TKey1, TKey2>
{
    private readonly Dictionary<TKey1, TKey2> _forwardDictionary = [];
    private readonly Dictionary<TKey2, TKey1> _reverseDictionary = [];
    private bool _isReadOnly;

    public Index<TKey1, TKey2> ForwardIndex { get; }

    public Index<TKey2, TKey1> ReverseIndex { get; }

    public Map()
    {
        ForwardIndex = new Index<TKey1, TKey2>(_forwardDictionary);
        ReverseIndex = new Index<TKey2, TKey1>(_reverseDictionary);
    }

    public Map(IDictionary<TKey1, TKey2> dictionary)
        : this()
    {
        foreach (KeyValuePair<TKey1, TKey2> d in dictionary)
            Add(d.Key, d.Value);
    }

    public void Add(TKey1 t1, TKey2 t2)
    {
        if (_isReadOnly)
            throw new InvalidOperationException("Map is read-only");

        _forwardDictionary.Add(t1, t2);

        try
        {
            _reverseDictionary.Add(t2, t1);
        }
        catch
        {
            try
            {
                _forwardDictionary.Remove(t1);
            }
            catch
            {
                //ignored
            }

            throw;
        }
    }

    public void Clear()
    {
        if (_isReadOnly)
            throw new InvalidOperationException("Map is read-only");

        _forwardDictionary.Clear();
        _reverseDictionary.Clear();
    }

    public void SetReadOnly() => _isReadOnly = true;
}