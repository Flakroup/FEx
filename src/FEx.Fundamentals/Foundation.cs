using FEx.Abstractions;
using FEx.Basics;
using FEx.Extensions;
using FEx.Fundamentals.Helpers;
using FEx.Fundamentals.StackTraces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace FEx.Fundamentals;

public class Foundation
{
    private static AsyncHelper _asyncHelper;
    private static IExceptionHandler _exceptionHandler;
    private static IFExServiceProvider _serviceProvider;
    private static IFExServiceProvider _strongInjectServiceProvider;
    private static ILogger _logger;
    private static IFExDispatcher _dispatcher;
    private static Thread _mainThread;
    private static SynchronizationContext _mainSynchronizationContext;

    public static IFExDispatcher Dispatcher
    {
        get => _dispatcher.Guard();
        private set => _dispatcher = value;
    }

    public static ILogger Logger
    {
        get => _logger.Guard();
        private set => _logger = value;
    }

    public static AsyncHelper AsyncHelper
    {
        get => _asyncHelper.Guard();
        private set => _asyncHelper = value;
    }

    public static IFExServiceProvider ServiceProvider
    {
        get => _serviceProvider.Guard();
        private set => _serviceProvider = value;
    }

    public static IFExServiceProvider StrongInjectServiceProvider
    {
        get => _strongInjectServiceProvider.Guard();
        private set => _strongInjectServiceProvider = value;
    }

    public static IExceptionHandler ExceptionHandler
    {
        get => _exceptionHandler.Guard();
        private set => _exceptionHandler = value;
    }

    public static bool SendEventsInCreationContext { get; set; }

    public static Thread MainThread
    {
        get => _mainThread.Guard();
        private set => _mainThread = value;
    }

    public static SynchronizationContext MainSynchronizationContext =>
        _mainSynchronizationContext ??= MainThread.GetThreadSynchronizationContext();

    static Foundation()
    {
        FExBasics.Init(new StackTraceGenerator());
    }

    public static void Init<T>(T strongInjectServiceProvider) where T : class, IFExServiceProvider
    {
        StrongInjectServiceProvider = strongInjectServiceProvider;
        StrongInjectServiceProvider.GetRequiredService<Foundation>().Guard();
        ServiceProvider = StrongInjectServiceProvider;
    }

    public static void RegisterDependencies(Func<IFExServiceProvider> serviceProviderConfiguration)
    {
        serviceProviderConfiguration.Guard(nameof(serviceProviderConfiguration));
        ServiceProvider = serviceProviderConfiguration();
    }

    public static void SetMainThread(bool ensureSyncContextExists = false)
    {
        if (_mainThread is not null)
            return;

        Thread currentThread = Thread.CurrentThread;
        bool isMainThread = currentThread.GetApartmentState() == ApartmentState.STA
                            && !currentThread.IsBackground
                            && currentThread.IsAlive
                            && !currentThread.IsThreadPoolThread;

        if (!isMainThread)
            throw new InvalidOperationException("This method must be called from main thread.");

        MainThread = currentThread;

        if (ensureSyncContextExists)
            MainThread.GetThreadSynchronizationContext(true);
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
        SetMainThread();
    }

    public Thread GetMainThread() => MainThread;
}