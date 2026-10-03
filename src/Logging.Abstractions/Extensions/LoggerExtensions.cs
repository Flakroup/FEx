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

    public static void Log(this ILogger logger, LogLevel logLevel, string message, Exception? exception = null)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                logger.LogTrace(exception, "{Message}", message);

                break;
            case LogLevel.Debug:
                logger.LogDebug(exception, "{Message}", message);

                break;
            case LogLevel.Information:
                logger.LogInformation(exception, "{Message}", message);

                break;
            case LogLevel.Warning:
                logger.LogWarning(exception, "{Message}", message);

                break;
            case LogLevel.Error:
                logger.LogError(exception, "{Message}", message);

                break;
            case LogLevel.Critical:
                logger.LogCritical(exception, "{Message}", message);

                break;
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public static void LogError<T>(this ILogger logger, T exception) where T : Exception =>
        logger.LogError(exception, "{Exception}", exception.ToString());

    public static ILogger GetMicrosoftLogger(this object sender) =>
        FExLoggingStatics.LoggerFactory.CreateLogger(sender.GetType());

    public static Serilog.ILogger GetSerilogLogger(this object sender) =>
        Serilog.Log.Logger.ForContext(sender.GetType());
}