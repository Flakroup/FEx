using FEx.Agnostics.Abstractions.Interfaces.Collections;
using System;
using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Collections;

/// <summary>A bidirectional map of unique forward keys and unique reverse keys that can be frozen against changes.</summary>
/// <typeparam name="TForwardKey">The forward key type.</typeparam>
/// <typeparam name="TReverseKey">The reverse key type.</typeparam>
public class Map<TForwardKey, TReverseKey> : IMap<TForwardKey, TReverseKey>
    where TForwardKey : notnull
    where TReverseKey : notnull
{
    private readonly Dictionary<TForwardKey, TReverseKey> _forwardDictionary = [];
    private readonly Dictionary<TReverseKey, TForwardKey> _reverseDictionary = [];
    private bool _isReadOnly;

    /// <inheritdoc />
    public IIndex<TForwardKey, TReverseKey> ForwardIndex { get; }

    /// <inheritdoc />
    public IIndex<TReverseKey, TForwardKey> ReverseIndex { get; }

    /// <summary>Initializes an empty map.</summary>
    public Map()
    {
        ForwardIndex = new Index<TForwardKey, TReverseKey>(_forwardDictionary);
        ReverseIndex = new Index<TReverseKey, TForwardKey>(_reverseDictionary);
    }

    /// <summary>Initializes a map from a dictionary.</summary>
    /// <param name="dictionary">The pairs to add.</param>
    /// <param name="isReadOnly">When true the map is made read-only after the pairs are added.</param>
    public Map(IDictionary<TForwardKey, TReverseKey> dictionary, bool isReadOnly = false)
        : this()
    {
        foreach (var d in dictionary)
            Add(d.Key, d.Value);

        if (isReadOnly)
            SetReadOnly();
    }

    /// <summary>Adds a pair of keys, rolling back when the reverse key already exists.</summary>
    /// <param name="t1">The forward key.</param>
    /// <param name="t2">The reverse key.</param>
    /// <exception cref="InvalidOperationException">The map is read-only.</exception>
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

    /// <summary>Removes all pairs.</summary>
    /// <exception cref="InvalidOperationException">The map is read-only.</exception>
    public void Clear()
    {
        if (_isReadOnly)
            throw new InvalidOperationException("Map is read-only");

        _forwardDictionary.Clear();
        _reverseDictionary.Clear();
    }

    /// <inheritdoc />
    public void SetReadOnly() => _isReadOnly = true;
}