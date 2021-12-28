using FEx.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace FEx.Logging;

public class Loggable : ILoggable
{
    private readonly ILogger _logger;

    public Loggable(ILogger logger)
    {
        _logger = logger;
    }

    protected IDisposable Scope { get; private set; }
    protected object State { get; private set; }

    public void LogCritical(string message, Exception exception = null)
    {
        _logger.LogCritical(exception, Combine(message));
    }

    public void LogDebug(string message, Exception exception = null)
    {
        _logger.LogDebug(exception, Combine(message));
    }

    public void LogError(string message, Exception exception = null)
    {
        _logger.LogError(exception, Combine(message));
    }

    public void LogInformation(string message, Exception exception = null)
    {
        _logger.LogInformation(exception, Combine(message));
    }

    public void LogTrace(string message, Exception exception = null)
    {
        _logger.LogTrace(exception, Combine(message));
    }

    public void LogWarning(string message, Exception exception = null)
    {
        _logger.LogWarning(exception, Combine(message));
    }

    public void BeginLabeledScope(params (string, object)[] state)
    {
        BeginLabeledScope(new LoggerState(state));
    }

    public void BeginLabeledScope(IDictionary<string, object> argsCustom)
    {
        BeginLabeledScope(new LoggerState(argsCustom));
    }

    public void Log(LogLevel logLevel, string message, Exception exception = null)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                LogTrace(message, exception);
                break;
            case LogLevel.Debug:
                LogDebug(message, exception);
                break;
            case LogLevel.Information:
                LogInformation(message, exception);
                break;
            case LogLevel.Warning:
                LogWarning(message, exception);
                break;
            case LogLevel.Error:
                LogError(message, exception);
                break;
            case LogLevel.Critical:
                LogCritical(message, exception);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public void BeginScope<TState>(TState state)
    {
        State = state;
        Scope = _logger.BeginScope(state);
    }

    public void BeginLabeledScope(ILoggerState state)
    {
        BeginScope(state);
    }

    public void AddOrUpdateLabel(string key, object value)
    {
        var lS = State as LoggerState;
        lS?.AddOrUpdateLabel(key, value);
    }

    public void RemoveLabel(string key)
    {
        var lS = State as LoggerState;
        lS?.RemoveLabel(key);
    }

    public void EndScope()
    {
        if (State != null)
        {
            State = null;
        }

        Scope?.Dispose();
    }

    private string Combine(string message)
    {
        return State == null ? message : $"[State:{State}]   {message}";
    }
}