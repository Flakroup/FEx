using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>
/// Represents a mutable dictionary of logging state/labels for structured logging scopes.
/// </summary>
[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface ILoggerState : IDictionary<string, object>, IDictionary, IReadOnlyDictionary<string, object>,
    ISerializable, IDeserializationCallback
{
    /// <summary>Adds a label or replaces the value of an existing one</summary>
    /// <param name="key">The label name.</param>
    /// <param name="value">The label value.</param>
    void AddOrUpdateLabel(string key, object value);
    /// <summary>Removes a label</summary>
    /// <param name="key">The label name.</param>
    void RemoveLabel(string key);
}