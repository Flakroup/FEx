namespace FEx.Agnostics.Abstractions.Interfaces.Collections;

/// <summary>A bidirectional map between two sets of unique keys.</summary>
/// <typeparam name="TForwardKey">The forward key type.</typeparam>
/// <typeparam name="TReverseKey">The reverse key type.</typeparam>
public interface IMap<TForwardKey, TReverseKey>
{
    /// <summary>Gets the lookup from forward keys to reverse keys.</summary>
    IIndex<TForwardKey, TReverseKey> ForwardIndex { get; }

    /// <summary>Gets the lookup from reverse keys to forward keys.</summary>
    IIndex<TReverseKey, TForwardKey> ReverseIndex { get; }

    /// <summary>Adds a pair of keys.</summary>
    /// <param name="t1">The forward key.</param>
    /// <param name="t2">The reverse key.</param>
    void Add(TForwardKey t1, TReverseKey t2);

    /// <summary>Removes all pairs.</summary>
    void Clear();

    /// <summary>Prevents further changes to the map.</summary>
    void SetReadOnly();
}