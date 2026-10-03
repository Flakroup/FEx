using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>A dictionary used as an in-memory cache that supports atomic add-or-update.</summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExMemoryCache<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary where TKey : notnull
{
    /// <summary>Adds a value for a key, or updates the existing value using a factory.</summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="addValue">The value to add when the key is absent.</param>
    /// <param name="updateValueFactory">Computes the new value from the key and the existing value.</param>
    /// <returns>The value now stored for the key.</returns>
    TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory);
}