using FEx.Abstractions;
using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using StrongInject;
using System;
using System.Linq;
using System.Reflection;

namespace FEx.Logging;

[Register(typeof(Loggable), typeof(ILoggable))]
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
}