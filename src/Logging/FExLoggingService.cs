using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;

namespace FEx.Logging;

public class FExLoggingService : IFExLoggingService
{
    protected ILogger DefaultLogger { get; }
    protected IFExLogger Logger { get; }

    public FExLoggingService(ILogger<FExLoggingService> defaultLogger, IFExLogger logger)
    {
        DefaultLogger = defaultLogger;
        Logger = logger;
    }

    public IFExLogger GetLogger<T>() => Logger;

    public IFExLogger GetLogger(object sender) => Logger;

    public void LogCritical<T>(string message, Exception exception = null)
    {
        if (exception is null)
            Logger.Critical(message);
        else
            Logger.Critical(exception, message);
    }

    public void LogDebug<T>(string message, Exception exception = null)
    {
        if (exception is null)
            Logger.Debug(message);
        else
            Logger.Debug(exception, message);
    }

    public void LogError<T>(string message, Exception exception = null)
    {
        if (exception is null)
            Logger.Error(message);
        else
            Logger.Error(exception, message);
    }

    public void LogInformation<T>(string message, Exception exception = null)
    {
        if (exception is null)
            Logger.Information(message);
        else
            Logger.Information(exception, message);
    }

    public void LogTrace<T>(string message, Exception exception = null)
    {
        if (exception is null)
            Logger.Trace(message);
        else
            Logger.Trace(exception, message);
    }

    public void LogWarning<T>(string message, Exception exception = null)
    {
        if (exception is null)
            Logger.Warning(message);
        else
            Logger.Warning(exception, message);
    }

    public void Log<T>(LogLevel logLevel, string message, Exception exception = null)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                LogTrace<T>(message, exception);

                break;
            case LogLevel.Debug:
                LogDebug<T>(message, exception);

                break;
            case LogLevel.Information:
                LogInformation<T>(message, exception);

                break;
            case LogLevel.Warning:
                LogWarning<T>(message, exception);

                break;
            case LogLevel.Error:
                LogError<T>(message, exception);

                break;
            case LogLevel.Critical:
                LogCritical<T>(message, exception);

                break;
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public void LogCritical(object sender, string message, Exception exception = null) =>
        LogCritical<object>(message, exception);

    public void LogDebug(object sender, string message, Exception exception = null) =>
        LogDebug<object>(message, exception);

    public void LogError(object sender, string message, Exception exception = null) =>
        LogError<object>(message, exception);

    public void LogInformation(object sender, string message, Exception exception = null) =>
        LogInformation<object>(message, exception);

    public void LogTrace(object sender, string message, Exception exception = null) =>
        LogTrace<object>(message, exception);

    public void LogWarning(object sender, string message, Exception exception = null) =>
        LogWarning<object>(message, exception);

    public void Log(object sender, LogLevel logLevel, string message, Exception exception = null) =>
        Log<object>(logLevel, message, exception);
}