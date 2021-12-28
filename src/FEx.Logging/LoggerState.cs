using FEx.Logging.Abstractions;

namespace FEx.Logging;

public class LoggerState : Dictionary<string, object>, ILoggerState
{
    public LoggerState(params (string, object)[] state)
        : this(state.ToDictionary(x => x.Item1, x => x.Item2))
    {
    }

    public LoggerState(IDictionary<string, object> dictionary)
        : base(dictionary)
    {
    }

    public void AddOrUpdateLabel(string key, object value)
    {
        this.AddOrUpdateValue(key, value);
    }

    public void RemoveLabel(string key)
    {
        this.RemoveValue(key);
    }

    public override string ToString()
    {
        return this.IsNotNullOrEmptyCollection() ? this.ToJson() : string.Empty;
    }
}