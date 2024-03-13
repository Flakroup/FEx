using FEx.Basics;
using FEx.Extensions;
using FEx.Extensions.Base.Converters;
using FEx.Extensions.Base.Enums;
using FEx.Extensions.Collections.Lists;
using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FEx.Logging.Extensions;

public static class LoggerExtensions
{
    public static string DefaultConsoleOutputTemplate { get; set; } =
        "[{Timestamp:HH:mm:ss}|{Level:u3}] <s:{SourceContext}>{NewLine}   {Message:lj}  {Exception}{NewLine}";

    public static string DefaultFileOutputTemplate { get; set; } =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss}|{Level:u3}] <s:{SourceContext}>{NewLine}   {Message:lj} {Exception}{NewLine}    [Properties:{Properties}]{NewLine}";

    public static IList<string> DefaultOverrides { get; set; } =
        ["Microsoft", "Microsoft.Hosting.Lifetime", "System"];

    public static LoggerConfiguration ConfigureSerilog(this LoggerConfiguration cfg,
                                                       string logFilePath,
                                                       bool forceConsole = false,
                                                       LogEventLevel externalLoggingLevel = LogEventLevel.Warning,
                                                       LogEventLevel externalDebugLoggingLevel =
                                                           LogEventLevel.Information,
                                                       Func<LoggerConfiguration, LoggerConfiguration> cfgFunc = null,
                                                       params string[] overrides)
    {
        logFilePath ??= FExBasics.AppInfoProvider.LogFilePath.Guard(nameof(logFilePath));

        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

        if (Debugger.IsAttached)
        {
            cfg = cfg.MinimumLevel.Debug()
                .AddOverrides(overrides, externalDebugLoggingLevel)
                .Enrich.FromLogContext()
                .WriteTo.Debug(outputTemplate: DefaultConsoleOutputTemplate)
                .WriteTo.SetFileLogger(logFilePath)
                .WriteTo.Console(outputTemplate: DefaultConsoleOutputTemplate, theme: AnsiConsoleTheme.Code);
        }
        else
        {
            cfg = cfg.MinimumLevel.Information()
                .AddOverrides(overrides, externalLoggingLevel)
                .Enrich.FromLogContext()
                .WriteTo.Async(x => x.SetFileLogger(logFilePath));

            if (forceConsole)
                cfg = cfg.WriteTo.Console(outputTemplate: DefaultConsoleOutputTemplate, theme: AnsiConsoleTheme.Code);
        }

        return cfgFunc?.Invoke(cfg) ?? cfg;
    }

    public static LoggerConfiguration AddOverrides(this LoggerConfiguration cfg,
                                                   IList<string> overrides,
                                                   LogEventLevel level)
    {
        if (overrides.IsNullOrEmptyList())
            overrides = DefaultOverrides;

        return overrides.Aggregate(cfg, (current, o) => current.MinimumLevel.Override(o, level));
    }

    public static IServiceCollection ConfigureLogging(this IServiceCollection services)
    {
        // Creating a `LoggerProviderCollection` lets Serilog optionally write
        // events through other dynamically-added MEL ILoggerProviders.
        var providers = new LoggerProviderCollection();

        return services.AddSingleton(providers)
            .AddSingleton<ILoggerFactory>(sc =>
            {
                LoggerProviderCollection providerCollection = sc.GetRequiredService<LoggerProviderCollection>();
                var factory = new SerilogLoggerFactory(null, true, providerCollection);

                foreach (ILoggerProvider provider in sc.GetServices<ILoggerProvider>())
                    factory.AddProvider(provider);

                return factory;
            })
            .AddLogging(loggingBuilder => loggingBuilder.AddSerilog());
    }

    public static void SetLogger(string logFilePath = null,
                                 bool forceConsole = false,
                                 LogEventLevel externalLoggingLevel = LogEventLevel.Warning,
                                 LogEventLevel externalDebugLoggingLevel = LogEventLevel.Information,
                                 Func<LoggerConfiguration, LoggerConfiguration> cfgFunc = null,
                                 params string[] overrides) =>
        Serilog.Log.Logger = new LoggerConfiguration().ConfigureSerilog(logFilePath,
                forceConsole,
                externalLoggingLevel,
                externalDebugLoggingLevel,
                cfgFunc,
                overrides)
            .CreateLogger();

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

    private static LoggerConfiguration
        SetFileLogger(this LoggerSinkConfiguration sinkConfiguration, string logFilePath) =>
        sinkConfiguration.File(logFilePath,
            outputTemplate: DefaultFileOutputTemplate,
            fileSizeLimitBytes: (int)FileLengthConverter.ConvertFileLength(100, LengthType.Megabytes, LengthType.Bytes),
            rollingInterval: RollingInterval.Hour,
            retainedFileCountLimit: 48,
            retainedFileTimeLimit: TimeSpan.FromDays(2));
}