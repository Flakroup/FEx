using Microsoft.Extensions.Logging;
using System;

namespace FEx.Logging.Abstractions;

public static class LoggerExtensions
{
    public static void LogError<T>(this ILogger logger, T exception) where T : Exception
    {
        logger.LogError(exception, exception.Message);
    }
}