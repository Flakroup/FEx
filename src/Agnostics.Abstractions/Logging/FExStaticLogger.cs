using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using System;

namespace FEx.Agnostics.Abstractions.Logging;

/// <summary>Static facade over the configured <see cref="IFExLogger" /> for code that cannot use dependency injection; defaults to <see cref="FExDebugLogger" />.</summary>
public class FExStaticLogger : FExInitializable
{
    // Always assigned by the static constructor (and each instance ctor) before any read via Logger.
    private static IFExLogger _logger = null!;

    /// <summary>Occurs when the current logger raises an error or critical event.</summary>
    public static event EventHandler<FExErrorEventArgs>? ErrorLogged;

    /// <summary>Gets the currently configured logger.</summary>
    public static IFExLogger Instance => Logger;

    /// <summary>Gets the currently configured logger; setting it moves the <see cref="ErrorLogged" /> subscription to the new logger.</summary>
    protected static IFExLogger Logger
    {
        get => _logger;
        private set
        {
            if (_logger is not null)
                _logger.ErrorLogged -= OnErrorLogged;

            _logger = value;

            if (value is not null)
                _logger.ErrorLogged += OnErrorLogged;
        }
    }

    /// <summary>Initializes the instance and makes <paramref name="logger" /> the current static logger</summary>
    /// <param name="logger">The logger to use.</param>
    public FExStaticLogger(IFExLogger logger)
    {
        Logger = logger; //todo make sure it's replaced when Serilog is configured
    }

    static FExStaticLogger()
    {
        Logger = new FExDebugLogger();
    }

    /// <summary>Writes a debug-level message</summary>
    /// <param name="message">The message to log.</param>
    public static void Debug(string message) => Logger.Debug(message);

    /// <summary>Writes a debug-level entry for an exception using its message</summary>
    /// <param name="exception">The exception to log.</param>
    public static void Debug(Exception exception) => Debug(exception, null);

    /// <summary>Writes a debug-level message with an exception</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message; the exception message when null.</param>
    public static void Debug(Exception exception, string? message) =>
        Logger.Debug(exception, message ?? exception.Message);

    /// <summary>Writes a information-level message</summary>
    /// <param name="message">The message to log.</param>
    public static void Information(string message) => Logger.Information(message);

    /// <summary>Writes a information-level entry for an exception using its message</summary>
    /// <param name="exception">The exception to log.</param>
    public static void Information(Exception exception) => Information(exception, null);

    /// <summary>Writes a information-level message with an exception</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message; the exception message when null.</param>
    public static void Information(Exception exception, string? message) =>
        Logger.Information(exception, message ?? exception.Message);

    /// <summary>Writes a warning-level message</summary>
    /// <param name="message">The message to log.</param>
    public static void Warning(string message) => Logger.Warning(message);

    /// <summary>Writes a warning-level entry for an exception using its message</summary>
    /// <param name="exception">The exception to log.</param>
    public static void Warning(Exception exception) => Warning(exception, null);

    /// <summary>Writes a warning-level message with an exception</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message; the exception message when null.</param>
    public static void Warning(Exception exception, string? message) =>
        Logger.Warning(exception, message ?? exception.Message);

    /// <summary>Writes a error-level message</summary>
    /// <param name="message">The message to log.</param>
    public static void Error(string message) => Logger.Error(message);

    /// <summary>Writes a error-level entry for an exception using its message</summary>
    /// <param name="exception">The exception to log.</param>
    public static void Error(Exception exception) => Error(exception, null);

    /// <summary>Writes a error-level message with an exception</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message; the exception message when null.</param>
    public static void Error(Exception exception, string? message) =>
        Logger.Error(exception, message ?? exception.Message);

    /// <summary>Writes a critical-level message</summary>
    /// <param name="message">The message to log.</param>
    public static void Critical(string message) => Logger.Critical(message);

    /// <summary>Writes a critical-level entry for an exception using its message</summary>
    /// <param name="exception">The exception to log.</param>
    public static void Critical(Exception exception) => Critical(exception, null);

    /// <summary>Writes a critical-level message with an exception</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message; the exception message when null.</param>
    public static void Critical(Exception exception, string? message) =>
        Logger.Critical(exception, message ?? exception.Message);

    /// <summary>Keeps the current logger; equivalent to <see cref="Configure(Func{IFExLogger})" /> with no factory.</summary>
    public static void Configure() => Configure(null);

    /// <summary>Replaces the current logger with one created by a factory</summary>
    /// <param name="loggerFactory">The factory; when null the current logger is kept.</param>
    public static void Configure(Func<IFExLogger>? loggerFactory)
    {
        if (loggerFactory is not null)
            Logger = loggerFactory();
    }

    private static void OnErrorLogged(object? sender, FExErrorEventArgs e) => ErrorLogged?.Invoke(sender, e);
}