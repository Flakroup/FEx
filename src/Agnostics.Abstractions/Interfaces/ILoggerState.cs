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
    void AddOrUpdateLabel(string key, object value);
    void RemoveLabel(string key);
}