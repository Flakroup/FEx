using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using System;

namespace FEx.Agnostics.Abstractions.Logging;

public class FExStaticLogger : FExInitializable
{
    private static IFExLogger _logger;

    public static event EventHandler<FExErrorEventArgs> ErrorLogged;

    public static IFExLogger Instance => Logger;

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

    public FExStaticLogger(IFExLogger logger)
    {
        Logger = logger; //todo make sure it's replaced when Serilog is configured
    }

    static FExStaticLogger()
    {
        Logger = new FExDebugLogger();
    }

    public static void Debug(string message) => Logger.Debug(message);

    public static void Debug(Exception exception, string message = null) =>
        Logger.Debug(exception, message ?? exception.Message);

    public static void Information(string message) => Logger.Information(message);

    public static void Information(Exception exception, string message = null) =>
        Logger.Information(exception, message ?? exception.Message);

    public static void Warning(string message) => Logger.Warning(message);

    public static void Warning(Exception exception, string message = null) =>
        Logger.Warning(exception, message ?? exception.Message);

    public static void Error(string message) => Logger.Error(message);

    public static void Error(Exception exception, string message = null) =>
        Logger.Error(exception, message ?? exception.Message);

    public static void Critical(string message) => Logger.Critical(message);

    public static void Critical(Exception exception, string message = null) =>
        Logger.Critical(exception, message ?? exception.Message);

    public static void Configure(Func<IFExLogger> loggerFactory = null)
    {
        if (loggerFactory is not null)
            Logger = loggerFactory();
    }

    private static void OnErrorLogged(object sender, FExErrorEventArgs e) => ErrorLogged?.Invoke(sender, e);
}