using FEx.DependencyInjection.Abstractions.Interfaces;
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
[Register(typeof(FExLoggingModuleInitializer),
    Scope.SingleInstance,
    typeof(FExLoggingModuleInitializer),
    typeof(IInitializeModule))]
public class FExLoggingModule
{
    [Instance]
    public static ILoggerProvider[] LoggerProviders { get; set; } = [];

    [Factory(Scope.SingleInstance)]
    public static SerilogLoggerFactory GetSerilogLoggerFactory(LoggerProviderCollection providerCollection) =>
        new(null, true, providerCollection);

    [Factory]
    public static ILogger<T> CreateLogger<T>(SerilogLoggerFactory factory) => factory.CreateLogger<T>();

    public static ILogger<T> CreateLogger<T>() =>
        GetSerilogLoggerFactory(GetLoggerProviderCollection(LoggerProviders)).CreateLogger<T>();

    [Factory]
    public static ILogger CreateLogger(LoggerProviderCollection providerCollection) =>
        GetSerilogLoggerFactory(providerCollection).CreateLogger(string.Empty);

    public static ILogger CreateLogger(Type senderType)
    {
        SerilogLoggerFactory loggerFactory = GetSerilogLoggerFactory(GetLoggerProviderCollection(LoggerProviders));

        MethodInfo methodInfo = typeof(LoggerFactoryExtensions).GetMethods()
            .Single(x => x.Name == nameof(LoggerFactoryExtensions.CreateLogger) && x.IsGenericMethod);

        MethodInfo genericMethod = methodInfo.MakeGenericMethod(senderType);

        return (ILogger)genericMethod.Invoke(loggerFactory, [loggerFactory]);
    }

    [Factory(Scope.SingleInstance)]
    public static LoggerProviderCollection GetLoggerProviderCollection(ILoggerProvider[] loggerProviders)
    {
        var collection = new LoggerProviderCollection();

        foreach (ILoggerProvider loggerProvider in loggerProviders)
            collection.AddProvider(loggerProvider);

        return collection;
    }

    public static void AddServices(IFExLoggingModule container, IServiceCollection services)
    {
        services.AddTransientServiceUsingContainer<ILoggable>(container);
        services.AddTransientServiceUsingContainer<ILoggingService>(container);
    }
}