using Microsoft.Extensions.Logging;

namespace FEx.Logging.Abstractions;

public interface ILoggable
{
    void AddOrUpdateLabel(string key, object value);
    void BeginLabeledScope(IDictionary<string, object> argsCustom);
    void BeginLabeledScope(ILoggerState state);
    void BeginLabeledScope(params (string, object)[] state);
    void BeginScope<TState>(TState state);
    void EndScope();
    void Log(LogLevel logLevel, string message, Exception exception = null);
    void LogCritical(string message, Exception exception = null);
    void LogDebug(string message, Exception exception = null);
    void LogError(string message, Exception exception = null);
    void LogInformation(string message, Exception exception = null);
    void LogTrace(string message, Exception exception = null);
    void LogWarning(string message, Exception exception = null);
    void RemoveLabel(string key);
}