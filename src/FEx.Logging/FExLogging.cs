using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;

namespace FEx.Logging;

public class FExLogging
{
    public static ILoggingService LoggingSrv { get; private set; }

    public static void Init(ILoggingService loggingService, FExLoggingConfigurator configurator)
    {
        LoggingSrv = loggingService;
        configurator.ConfigureSerilog();
    }

    public static void Log(string message,
                           Type callerType,
                           LogLevel level = LogLevel.Information,
                           Exception exception = null)
    {
        LoggingSrv.Log(callerType, level, message, exception);
    }

    public static void Log<T>(string message, LogLevel level = LogLevel.Information, Exception exception = null)
    {
        LoggingSrv.Log<T>(level, message, exception);
    }
}