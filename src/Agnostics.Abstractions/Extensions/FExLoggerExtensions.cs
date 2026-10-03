using FEx.Agnostics.Abstractions.Interfaces;
using System;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions that log an exception without a custom message.</summary>
public static class FExLoggerExtensions
{
    /// <summary>Writes a trace-level entry for an exception</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    public static void Trace(this IFExLogger logger, Exception exception) => logger.Trace(exception, null);

    /// <summary>Writes a debug-level entry for an exception</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    public static void Debug(this IFExLogger logger, Exception exception) => logger.Debug(exception, null);

    /// <summary>Writes an information-level entry for an exception</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    public static void Information(this IFExLogger logger, Exception exception) => logger.Information(exception, null);

    /// <summary>Writes a warning-level entry for an exception</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    public static void Warning(this IFExLogger logger, Exception exception) => logger.Warning(exception, null);

    /// <summary>Writes an error-level entry for an exception</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    public static void Error(this IFExLogger logger, Exception exception) => logger.Error(exception, null);

    /// <summary>Writes a critical-level entry for an exception</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    public static void Critical(this IFExLogger logger, Exception exception) => logger.Critical(exception, null);
}