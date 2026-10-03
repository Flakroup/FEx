using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Interfaces.Collections;

/// <summary>A dictionary that serves as one lookup direction of a map.</summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public interface IIndex<TKey, TValue> : IDictionary<TKey, TValue>
{
}