using FEx.Logging.Abstractions;
using FEx.Logging.Abstractions.Extensions;
using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;

namespace FEx.Logging;

public class FExLoggingService : IFExLoggingService
{
    protected ILogger DefaultLogger { get; }

    public FExLoggingService(ILogger<FExLoggingService> defaultLogger)
    {
        DefaultLogger = defaultLogger;
    }

    public ILoggable GetLogger<T>()
    {
        ILogger logger = FExLoggingModule.CreateLogger<T>();

        return new Loggable(logger);
    }

    public ILoggable GetLogger(object sender)
    {
        if (sender is not Type senderType)
            senderType = sender.GetType();

        ILogger logger = FExLoggingModule.CreateLogger(senderType);

        return new Loggable(logger);
    }

    public void LogCritical<T>(string message, Exception exception = null) =>
        DoLoggableAct<T>(logger => logger.LogCritical(message, exception));

    public void LogDebug<T>(string message, Exception exception = null) =>
        DoLoggableAct<T>(logger => logger.LogDebug(message, exception));

    public void LogError<T>(string message, Exception exception = null) =>
        DoLoggableAct<T>(logger => logger.LogError(message, exception));

    public void LogInformation<T>(string message, Exception exception = null) =>
        DoLoggableAct<T>(logger => logger.LogInformation(message, exception));

    public void LogTrace<T>(string message, Exception exception = null) =>
        DoLoggableAct<T>(logger => logger.LogTrace(message, exception));

    public void LogWarning<T>(string message, Exception exception = null) =>
        DoLoggableAct<T>(logger => logger.LogWarning(message, exception));

    public void Log<T>(LogLevel logLevel, string message, Exception exception = null) =>
        DoLoggableAct<T>(logger => logger.Log(logLevel, message, exception));

    public void LogCritical(object sender, string message, Exception exception = null) =>
        DoLoggableAct(sender, logger => logger.LogCritical(message, exception));

    public void LogDebug(object sender, string message, Exception exception = null) =>
        DoLoggableAct(sender, logger => logger.LogDebug(message, exception));

    public void LogError(object sender, string message, Exception exception = null) =>
        DoLoggableAct(sender, logger => logger.LogError(message, exception));

    public void LogInformation(object sender, string message, Exception exception = null) =>
        DoLoggableAct(sender, logger => logger.LogInformation(message, exception));

    public void LogTrace(object sender, string message, Exception exception = null) =>
        DoLoggableAct(sender, logger => logger.LogTrace(message, exception));

    public void LogWarning(object sender, string message, Exception exception = null) =>
        DoLoggableAct(sender, logger => logger.LogWarning(message, exception));

    public void Log(object sender, LogLevel logLevel, string message, Exception exception = null) =>
        DoLoggableAct(sender, logger => logger.Log(logLevel, message, exception));

    private void DoLoggableAct<T>(Action<ILoggable> loggableAct) => loggableAct(GetLogger<T>());

    private void DoLoggableAct(object sender, Action<ILoggable> loggableAct) => loggableAct(GetLogger(sender));
}