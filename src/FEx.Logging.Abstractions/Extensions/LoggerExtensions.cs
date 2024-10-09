using Microsoft.Extensions.Logging;
using Serilog;
using System;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FEx.Logging.Abstractions.Extensions;

public static class LoggerExtensions
{
    public static void LogError<T>(this ILogger logger, T exception) where T : Exception =>
        logger.LogError(exception, exception.ToString());

    public static ILogger GetLogger(this object sender) =>
        FExLoggingFoundation.LoggerFactory.CreateLogger(sender.GetType());

    public static Serilog.ILogger GetSerilogLogger(this object sender) => Log.Logger.ForContext(sender.GetType());
}