using FEx.Agnostics.Abstractions.Interfaces.Collections;
using System;
using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Collections;

public class Map<TForwardKey, TReverseKey> : IMap<TForwardKey, TReverseKey>
{
    private readonly Dictionary<TForwardKey, TReverseKey> _forwardDictionary = [];
    private readonly Dictionary<TReverseKey, TForwardKey> _reverseDictionary = [];
    private bool _isReadOnly;

    public IIndex<TForwardKey, TReverseKey> ForwardIndex { get; }

    public IIndex<TReverseKey, TForwardKey> ReverseIndex { get; }

    public Map()
    {
        ForwardIndex = new Index<TForwardKey, TReverseKey>(_forwardDictionary);
        ReverseIndex = new Index<TReverseKey, TForwardKey>(_reverseDictionary);
    }

    public Map(IDictionary<TForwardKey, TReverseKey> dictionary, bool isReadOnly = false)
        : this()
    {
        foreach (KeyValuePair<TForwardKey, TReverseKey> d in dictionary)
            Add(d.Key, d.Value);

        if (isReadOnly)
            SetReadOnly();
    }

    public void Add(TForwardKey t1, TReverseKey t2)
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