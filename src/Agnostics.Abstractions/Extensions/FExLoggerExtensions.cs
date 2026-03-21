using FEx.Agnostics.Abstractions.Interfaces;
using System;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class FExLoggerExtensions
{
    public static void Trace(this IFExLogger logger, Exception exception) =>
        logger.Trace(exception, null);

    public static void Debug(this IFExLogger logger, Exception exception) =>
        logger.Debug(exception, null);

    public static void Information(this IFExLogger logger, Exception exception) =>
        logger.Information(exception, null);

    public static void Warning(this IFExLogger logger, Exception exception) =>
        logger.Warning(exception, null);

    public static void Error(this IFExLogger logger, Exception exception) =>
        logger.Error(exception, null);

    public static void Critical(this IFExLogger logger, Exception exception) =>
        logger.Critical(exception, null);
}
