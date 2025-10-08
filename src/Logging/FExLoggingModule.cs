using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Logging;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Logging.Abstractions;
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
using System.Reflection;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FEx.Logging;

[Register(typeof(FExLoggingModule),
    Scope.SingleInstance,
    typeof(IFExInitialize),
    typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(FExStaticLogger), Scope.SingleInstance, typeof(IFExInitializable))]
[Register(typeof(Loggable), typeof(ILoggable))]
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

        foreach (ILoggerProvider loggerProvider in loggerProviders)
            collection.AddProvider(loggerProvider);

        return collection;
    }

    [Factory]
    public static ILogger<T> CreateLogger<T>(ILoggerFactory factory) => factory.CreateLogger<T>();

    [Factory]
    public static ILogger CreateLogger(LoggerProviderCollection providerCollection) =>
        GetSerilogLoggerFactory(providerCollection).CreateLogger(string.Empty);

    public static ILogger<T> CreateLogger<T>() =>
        GetSerilogLoggerFactory(GetLoggerProviderCollection(LoggerProviders)).CreateLogger<T>();

    public static ILogger CreateLogger(Type senderType)
    {
        var loggerFactory = (SerilogLoggerFactory)GetSerilogLoggerFactory(GetLoggerProviderCollection(LoggerProviders));

        MethodInfo methodInfo = typeof(LoggerFactoryExtensions).GetMethods()
            .Single(static x => x.Name == nameof(LoggerFactoryExtensions.CreateLogger) && x.IsGenericMethod);

        MethodInfo genericMethod = methodInfo.MakeGenericMethod(senderType);

        return (ILogger)genericMethod.Invoke(loggerFactory, [loggerFactory]);
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

        string latestLogPath =
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

        services.AddTransientServiceUsingContainer<ILoggable>(container);
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