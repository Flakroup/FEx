using FEx.DI.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Interfaces;
using FEx.Logging.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FEx.Logging;

[Register(typeof(Loggable), typeof(ILoggable))]
[Register(typeof(LoggingService), Scope.SingleInstance, typeof(ILoggingService))]
[Register(typeof(FExLoggingConfigurator), Scope.SingleInstance)]
[Register(typeof(FExLogging), Scope.SingleInstance, typeof(FExLogging), typeof(IInitializeModule))]
public class FExLoggingModule
{
    [Instance]
    public static ILoggerProvider[] LoggerProviders { get; set; } = [];

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
            .Single(x => x.Name == nameof(LoggerFactoryExtensions.CreateLogger) && x.IsGenericMethod);

        MethodInfo genericMethod = methodInfo.MakeGenericMethod(senderType);

        return (ILogger)genericMethod.Invoke(loggerFactory, [loggerFactory]);
    }

    public static void AddServices(IFExLoggingContainer container, IServiceCollection services)
    {
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());

        services.AddTransientServiceUsingContainer<ILoggable>(container);
        services.AddTransientServiceUsingContainer<ILoggingService>(container);
        services.AddTransientServiceUsingContainer<ILogger>(container);

        services.AddSingletonServiceUsingContainer<LoggerProviderCollection>(container);
        services.AddSingletonServiceUsingContainer<ILoggerFactory>(container);
        services.AddSingletonServiceUsingContainer<ILoggingService>(container);
    }
}