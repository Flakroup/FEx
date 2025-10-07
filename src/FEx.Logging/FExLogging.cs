using FEx.Common.Extensions;
using FEx.DI.Abstractions;
using FEx.Fundamentals;
using FEx.Json;
using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Diagnostics;

namespace FEx.Logging;

public class FExLogging : InitializeModule<IFExLoggingContainer>
{
    private static ILoggingService _loggingSrv;

    private static FExLoggingConfigurator _configuration;

    public static ILoggingService LoggingSrv
    {
        get => _loggingSrv.Guard();
        private set => _loggingSrv = value.Guard(nameof(value));
    }

    public static FExLoggingConfigurator Configurator
    {
        get => _configuration.Guard();
        private set => _configuration = value.Guard(nameof(value));
    }

    public FExLogging(FExFundamentals fundamentalsModule,
                      FExJson jsonModule,
                      ILoggingService loggingService,
                      FExLoggingConfigurator configurator)
        : base(fundamentalsModule, jsonModule)
    {
        LoggingSrv = loggingService;
        Configurator = configurator;
    }

    public static void Log(string message,
                           Type callerType,
                           LogLevel level = LogLevel.Information,
                           Exception exception = null) =>
        LoggingSrv.Log(callerType, level, message, exception);

    public static void Log<T>(string message, LogLevel level = LogLevel.Information, Exception exception = null) =>
        LoggingSrv.Log<T>(level, message, exception);

    public static LoggerConfiguration Configure(Action<FExLoggingConfigurator> configuration = null)
    {
        configuration?.Invoke(Configurator);

        return Configurator.ConfigureSerilog().Configuration;
    }

    public static void OpenLogFile() => Process.Start(Configurator.LogFilePath)?.Dispose();

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Configure();
    }

    protected override void AddServices(IFExLoggingContainer container, IServiceCollection services) =>
        FExLoggingModule.AddServices(container, services);
}