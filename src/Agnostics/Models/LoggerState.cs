using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FEx.Agnostics.Models;

/// <summary>
/// Default implementation of ILoggerState for structured logging scopes.
/// </summary>
public class LoggerState : Dictionary<string, object>, ILoggerState
{
    /// <summary>Initializes the state from key and value tuples.</summary>
    /// <param name="state">The initial key and value pairs.</param>
    /// <exception cref="System.ArgumentException">Two tuples share the same key.</exception>
    public LoggerState(params (string, object)[] state)
        : this(state.ToDictionary(static x => x.Item1, static x => x.Item2))
    {
    }

    /// <summary>Initializes the state with a copy of the given dictionary.</summary>
    /// <param name="dictionary">The initial entries.</param>
    public LoggerState(IDictionary<string, object> dictionary)
        : base(dictionary)
    {
    }

    /// <summary>Adds a label or replaces the value of an existing one.</summary>
    /// <param name="key">The label name.</param>
    /// <param name="value">The label value.</param>
    public void AddOrUpdateLabel(string key, object value) => this.AddOrUpdateValue(key, value);

    /// <summary>Removes a label if it exists.</summary>
    /// <param name="key">The label name.</param>
    public void RemoveLabel(string key) => this.RemoveValue(key);

    /// <summary>Serializes the state to JSON.</summary>
    /// <returns>The JSON text, or an empty string when there are no labels.</returns>
    public override string ToString() =>
        this.IsNotNullOrEmptyCollection()
            ? JsonSerializer.Serialize(this)
            : string.Empty;
}