using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;

namespace FEx.Logging;

public class FExLogging
{
    public static ILoggingService LoggingSrv { get; private set; }

    public static FExLoggingConfigurator Configuration { get; private set; }

    public static void Init(ILoggingService loggingService, FExLoggingConfigurator configurator)
    {
        LoggingSrv = loggingService;
        configurator.ConfigureSerilog();
    }

    public static void Log(string message,
                           Type callerType,
                           LogLevel level = LogLevel.Information,
                           Exception exception = null) => LoggingSrv.Log(callerType, level, message, exception);

    public static void Log<T>(string message, LogLevel level = LogLevel.Information, Exception exception = null) => LoggingSrv.Log<T>(level, message, exception);

    public static void Configure(bool forceConsole = false,
                                 LogEventLevel externalLoggingLevel = LogEventLevel.Warning,
                                 LogEventLevel externalDebugLoggingLevel = LogEventLevel.Information,
                                 Func<LoggerConfiguration, LoggerConfiguration> cfgFunc = null,
                                 params string[] overrides) => Configuration = new FExLoggingConfigurator().Set(
                forceConsole,
                externalLoggingLevel,
                externalDebugLoggingLevel,
                cfgFunc,
                overrides)
            .ConfigureSerilog();
}