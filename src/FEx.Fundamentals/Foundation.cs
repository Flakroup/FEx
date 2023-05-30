using FEx.Abstractions;
using FEx.Extensions;
using FEx.Fundamentals.StackTraces;
using Microsoft.Extensions.Logging;
using System;

namespace FEx.Fundamentals;

public class Foundation
{
    public static IFExDispatcher Dispatcher { get; private set; }
    public static ILogger Logger { get; private set; }
    public static AsyncHelper AsyncHelper { get; private set; }
    public static IFExServiceProvider ServiceProvider { get; private set; }
    public static IExceptionHandler ExceptionHandler { get; private set; }

    public static StackTraceGenerator StackTraceGenerator { get; }
    public static bool SendEventsInCreationContext { get; set; }

    static Foundation()
    {
        StackTraceGenerator = new();
    }

    public Foundation(IFExDispatcher dispatcher,
                      ILogger<Foundation> logger,
                      AsyncHelper asyncHelper,
                      IExceptionHandler exceptionHandler)
    {
        Dispatcher = dispatcher;
        Logger = logger;
        AsyncHelper = asyncHelper;
        ExceptionHandler = exceptionHandler;
    }

    public static void Init(Func<IFExServiceProvider> serviceProviderConfiguration)
    {
        serviceProviderConfiguration.Guard(nameof(serviceProviderConfiguration));
        ServiceProvider = serviceProviderConfiguration();
        ServiceProvider.GetRequiredService<Foundation>();
    }
}