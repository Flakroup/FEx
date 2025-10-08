using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FEx.Logging.Abstractions.Extensions;

public static class LoggerExtensions
{
    public static LoggerConfiguration AddOverrides(this LoggerConfiguration cfg,
                                                   IList<string> overrides,
                                                   LogEventLevel level)
    {
        return overrides.Aggregate(cfg, (current, o) => current.MinimumLevel.Override(o, level));
    }

    public static void Log(this ILogger logger, LogLevel logLevel, string message, Exception exception = null)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                logger.LogTrace(message, exception);

                break;
            case LogLevel.Debug:
                logger.LogDebug(message, exception);

                break;
            case LogLevel.Information:
                logger.LogInformation(message, exception);

                break;
            case LogLevel.Warning:
                logger.LogWarning(message, exception);

                break;
            case LogLevel.Error:
                logger.LogError(message, exception);

                break;
            case LogLevel.Critical:
                logger.LogCritical(message, exception);

                break;
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public static void Log(this ILoggable loggable, LogLevel logLevel, string message, Exception exception = null)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                loggable.LogTrace(message, exception);

                break;
            case LogLevel.Debug:
                loggable.LogDebug(message, exception);

                break;
            case LogLevel.Information:
                loggable.LogInformation(message, exception);

                break;
            case LogLevel.Warning:
                loggable.LogWarning(message, exception);

                break;
            case LogLevel.Error:
                loggable.LogError(message, exception);

                break;
            case LogLevel.Critical:
                loggable.LogCritical(message, exception);

                break;
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public static void LogError<T>(this ILogger logger, T exception) where T : Exception =>
        logger.LogError(exception, exception.ToString());

    public static ILoggable GetLogger(this object sender) => new Loggable(sender.GetMicrosoftLogger());

    public static ILoggable GetLogger<T>() => new Loggable(FExLoggingStatics.LoggerFactory.CreateLogger<T>());

    public static ILogger GetMicrosoftLogger(this object sender) =>
        FExLoggingStatics.LoggerFactory.CreateLogger(sender.GetType());

    public static Serilog.ILogger GetSerilogLogger(this object sender) =>
        Serilog.Log.Logger.ForContext(sender.GetType());
}