using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Logging;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Interfaces;
using FEx.Logging.Sinks.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FEx.Logging;

[Register(typeof(FExLoggingModule),
    Scope.SingleInstance,
    typeof(IFExInitializable),
    typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(FExStaticLogger), Scope.SingleInstance, typeof(IFExInitializable))]
[Register(typeof(FExLoggingService), Scope.SingleInstance, typeof(IFExLoggingService))]
[Register(typeof(FExSerilogLogger), typeof(IFExLogger))]
[Register(typeof(FExLoggingConfigurator), typeof(IFExLoggingConfigurator), typeof(IConfigurator))]
[Register(typeof(LoggingConfiguration), Scope.SingleInstance, typeof(ILoggingConfiguration))]
[Register(typeof(AsyncFileSinkConfigurator), typeof(ISinkConfigurator))]
[Register(typeof(ConsoleSinkConfigurator), typeof(ISinkConfigurator))]
[Register(typeof(DebugSinkConfigurator), typeof(ISinkConfigurator))]
[Register(typeof(PlatformSinkConfigurator), typeof(ISinkConfigurator))]
[Register(typeof(DefaultPlatformLogger), typeof(IPlatformLogger))]
public class FExLoggingModule : InitializeModule<IFExLoggingContainer, IServiceCollection>
{
    private static IFExLoggingService _loggingSrv;
    private static IFExLoggingConfigurator _configuration;

    [Instance]
    public static ILoggerProvider[] LoggerProviders { get; set; } = [];

    public static IFExLoggingService LoggingSrv
    {
        get => _loggingSrv.GuardProperty();
        private set => _loggingSrv = value.Guard(nameof(value));
    }

    public static IFExLoggingConfigurator Configurator
    {
        get => _configuration.GuardProperty();
        private set => _configuration = value.Guard(nameof(value));
    }

    public FExLoggingModule(IFExLoggingService loggingService, IFExLoggingConfigurator configurator)
    {
        LoggingSrv = loggingService;
        Configurator = configurator;
    }

    [Factory(Scope.SingleInstance)]
    public static ILoggerFactory GetSerilogLoggerFactory(LoggerProviderCollection providerCollection) =>
        new SerilogLoggerFactory(null, true, providerCollection);

    /// <summary>
    /// Creating a `LoggerProviderCollection` lets Serilog optionally write
    /// events through other dynamically-added MEL ILoggerProviders.
    /// </summary>
    [Factory(Scope.SingleInstance)]
    public static LoggerProviderCollection GetLoggerProviderCollection(ILoggerProvider[] loggerProviders)
    {
        var collection = new LoggerProviderCollection();

        foreach (var loggerProvider in loggerProviders)
            collection.AddProvider(loggerProvider);

        return collection;
    }

    [Factory]
    public static ILogger<T> CreateLogger<T>(ILoggerFactory factory) => factory.CreateLogger<T>();

    [Factory]
    public static ILogger CreateLogger(ILoggerFactory factory) =>
        factory.CreateLogger(string.Empty);

    private static ILoggerFactory _convenienceFactory;

    private static ILoggerFactory GetConvenienceFactory() =>
#pragma warning disable IDISP004 // application-lifetime singleton
        _convenienceFactory ??= new SerilogLoggerFactory(null, false, GetLoggerProviderCollection(LoggerProviders));
#pragma warning restore IDISP004

    public static ILogger<T> CreateLogger<T>() =>
        GetConvenienceFactory().CreateLogger<T>();

    public static ILogger CreateLogger(Type senderType)
    {
        var factory = GetConvenienceFactory();

        var methodInfo = typeof(LoggerFactoryExtensions).GetMethods()
            .Single(static x => x.Name == nameof(LoggerFactoryExtensions.CreateLogger) && x.IsGenericMethod);

        var genericMethod = methodInfo.MakeGenericMethod(senderType);

        return (ILogger)genericMethod.Invoke(factory, [factory]);
    }

    public static void Log(string message,
                           Type callerType,
                           LogLevel level = LogLevel.Information,
                           Exception exception = null) =>
        LoggingSrv.Log(callerType, level, message, exception);

    public static void Log<T>(string message, LogLevel level = LogLevel.Information, Exception exception = null) =>
        LoggingSrv.Log<T>(level, message, exception);

    public static LoggerConfiguration Configure()
    {
        Configurator.Configure();

        return Configurator.Configuration;
    }

    public static void OpenLogFile()
    {
        FileInfo[] logs = [.. Configurator.GetLogFiles()];

        if (!logs.Any())
            return;

        var latestLogPath =
#if NETSTANDARD
            logs.OrderByDescending(static f => f.LastWriteTimeUtc).First().FullName;
#else
            logs.MaxBy(static f => f.LastWriteTimeUtc).FullName;
#endif

        using var _ = Process.Start(latestLogPath);
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Configure();
    }

    protected override void RegisterServices(IFExLoggingContainer container, IServiceCollection services)
    {
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());

        services.AddTransientServiceUsingContainer<IFExLoggingService>(container);
        services.AddTransientServiceUsingContainer<IFExLogger>(container);
        services.AddTransientServiceUsingContainer<ILogger>(container);
        services.AddTransientServiceUsingContainer<IFExLoggingConfigurator>(container);
        services.AddTransientServiceUsingContainer<ILoggingConfiguration>(container);
        services.AddTransientServiceUsingContainer<ISinkConfigurator[]>(container);
        services.AddTransientServiceUsingContainer<IPlatformLogger>(container);

        services.AddSingletonServiceUsingContainer<LoggerProviderCollection>(container);
        services.AddSingletonServiceUsingContainer<ILoggerFactory>(container);
    }
}