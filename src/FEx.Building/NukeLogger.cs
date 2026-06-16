using Microsoft.Extensions.Logging;
using Serilog.Events;
using System;
using System.Threading;

namespace FEx.Building;

public static class NukeLogger
{
    public static ILogger<T> Get<T>() => new NukeLogger<T>();
}

public class NukeLogger<T> : ILogger<T>
{
    protected SemaphoreSlim? Scope { get; set; }
    protected object? State { get; set; }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        State = state;
        Scope = new(1, 1);

        return Scope;
    }

    public void Log<TState>(LogLevel logLevel,
                            EventId eventId,
                            TState state,
                            Exception? exception,
                            Func<TState, Exception?, string> formatter) =>
        Serilog.Log.Write(GetSerilogLogLevel(logLevel), formatter(state, exception));

    public bool IsEnabled(LogLevel logLevel) => Serilog.Log.IsEnabled(GetLogLevel(logLevel));

    private static LogEventLevel GetLogLevel(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information or LogLevel.None => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };

    private static LogEventLevel GetSerilogLogLevel(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace or LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information or LogLevel.None => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error or LogLevel.Critical => LogEventLevel.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
}