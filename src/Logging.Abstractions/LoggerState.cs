using FEx.Agnostics.Abstractions.Extensions;
using FEx.Logging.Abstractions.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FEx.Logging.Abstractions;

public class LoggerState : Dictionary<string, object>, ILoggerState
{
    public LoggerState(params (string, object)[] state)
        : this(state.ToDictionary(static x => x.Item1, static x => x.Item2))
    {
    }

    public LoggerState(IDictionary<string, object> dictionary)
        : base(dictionary)
    {
    }

    public void AddOrUpdateLabel(string key, object value) => this.AddOrUpdateValue(key, value);

    public void RemoveLabel(string key) => this.RemoveValue(key);

    public override string ToString() =>
        this.IsNotNullOrEmptyCollection()
            ? JsonSerializer.Serialize(this)
            : string.Empty;
}