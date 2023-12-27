using FEx.Extensions;
using FEx.Extensions.Base.Converters;
using FEx.Extensions.Base.Enums;
using FEx.Extensions.Collections.Lists;
using FEx.Fundamentals;
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

namespace FEx.Logging;

public static class LoggerExtensions
{
    public static string DefaultConsoleOutputTemplate { get; set; } =
        "[{Timestamp:HH:mm:ss}|{Level:u3}] <s:{SourceContext}>{NewLine}   {Message:lj}  {Exception}{NewLine}";

    public static string DefaultFileOutputTemplate { get; set; } =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss}|{Level:u3}] <s:{SourceContext}>{NewLine}   {Message:lj} {Exception}{NewLine}    [Properties:{Properties}]{NewLine}";

    public static IList<string> DefaultOverrides { get; set; } =
        new[] { "Microsoft", "Microsoft.Hosting.Lifetime", "System" };

    public static LoggerConfiguration ConfigureSerilog(this LoggerConfiguration cfg,
                                                       string logFilePath,
                                                       bool forceConsole = false,
                                                       LogEventLevel externalLoggingLevel = LogEventLevel.Warning,
                                                       LogEventLevel externalDebugLoggingLevel =
                                                           LogEventLevel.Information,
                                                       Func<LoggerConfiguration, LoggerConfiguration> cfgFunc = null,
                                                       params string[] overrides)
    {
        logFilePath ??= Foundation.AppInfoProvider.LogFilePath.Guard(nameof(logFilePath));

        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

        if (Debugger.IsAttached)
        {
            cfg = cfg.MinimumLevel.Debug()
                .AddOverrides(overrides, externalDebugLoggingLevel)
                .Enrich.FromLogContext()
                .WriteTo.Debug(outputTemplate: DefaultConsoleOutputTemplate)
                .WriteTo.SetFileLogger(logFilePath)
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: DefaultConsoleOutputTemplate);
        }
        else
        {
            cfg = cfg.MinimumLevel.Information()
                .AddOverrides(overrides, externalLoggingLevel)
                .Enrich.FromLogContext()
                .WriteTo.Async(x => x.SetFileLogger(logFilePath));

            if (forceConsole)
                cfg = cfg.WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: DefaultConsoleOutputTemplate);
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
                                 params string[] overrides) => Log.Logger = new LoggerConfiguration()
        .ConfigureSerilog(logFilePath, forceConsole, externalLoggingLevel, externalDebugLoggingLevel, cfgFunc,
            overrides)
        .CreateLogger();

    private static LoggerConfiguration
        SetFileLogger(this LoggerSinkConfiguration sinkConfiguration, string logFilePath) => sinkConfiguration.File(
        logFilePath, rollingInterval: RollingInterval.Hour, retainedFileCountLimit: 48,
        retainedFileTimeLimit: TimeSpan.FromDays(2),
        fileSizeLimitBytes: (int)FileLengthConverter.ConvertFileLength(100, LengthType.Megabytes, LengthType.Bytes),
        outputTemplate: DefaultFileOutputTemplate);
}