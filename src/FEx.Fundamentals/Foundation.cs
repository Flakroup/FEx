using FEx.Abstractions;
using FEx.Basics;
using FEx.Basics.Interfaces;
using FEx.Extensions;
using FEx.Extensions.Base;
using FEx.Fundamentals.Helpers;
using FEx.Fundamentals.Utilities.OS;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace FEx.Fundamentals;

public class Foundation
{
    private static AsyncHelper _asyncHelper;
    private static IFExServiceProvider _serviceProvider;
    private static IFExServiceProvider _strongInjectServiceProvider;
    private static Thread _mainThread;
    private static SynchronizationContext _mainSynchronizationContext;

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

    public static bool SendEventsInCreationContext { get; set; }

    public static Thread MainThread
    {
        get => _mainThread.Guard();
        private set => _mainThread = value;
    }

    public static SynchronizationContext MainSynchronizationContext
    {
        get
        {
            if (IsDispatcherContext)
                return _mainSynchronizationContext;

            SynchronizationContext context = MainThread.GetThreadSynchronizationContext();

            if (context is not null)
            {
                _mainSynchronizationContext = context;

                if (_mainSynchronizationContext.GetType().Name == "DispatcherSynchronizationContext")
                    IsDispatcherContext = true;
            }

            return _mainSynchronizationContext;
        }
    }

    public static bool IsInitialized { get; private set; }

    private static bool IsDispatcherContext { get; set; }

    public Foundation(ILogger<FExBasics> logger,
                      AsyncHelper asyncHelper,
                      IExceptionHandler exceptionHandler,
                      IStackTraceProvider stackTraceProvider,
                      IEventDeliverer eventDeliverer)
    {
        AsyncHelper = asyncHelper;
        FExExtensionsCommon.Initialize(exceptionHandler.Guard(nameof(exceptionHandler)));
        FExBasics.Init(stackTraceProvider, eventDeliverer, logger);
    }

    public static void Init<T>(T strongInjectServiceProvider) where T : class, IFExServiceProvider
    {
        StrongInjectServiceProvider = strongInjectServiceProvider;
        StrongInjectServiceProvider.GetRequiredService<Foundation>().Guard();
        ServiceProvider = StrongInjectServiceProvider;
        IsInitialized = true;
    }

    public static void RegisterDependencies(Func<IFExServiceProvider> serviceProviderConfiguration)
    {
        serviceProviderConfiguration.Guard(nameof(serviceProviderConfiguration));
        ServiceProvider = serviceProviderConfiguration();
    }

    public static void SetMainThread(bool ensureSyncContextExists = false)
    {
        Thread currentThread = Thread.CurrentThread;

        bool isMainThread = IsPlatformMainThread(currentThread)
                            && !currentThread.IsBackground
                            && currentThread.IsAlive
                            && !currentThread.IsThreadPoolThread;

        if (!isMainThread)
            throw new InvalidOperationException("This method must be called from main thread.");

        MainThread = currentThread;

        if (ensureSyncContextExists)
            MainThread.GetThreadSynchronizationContext(true);
    }

    public Thread GetMainThread() => MainThread;

    private static bool IsPlatformMainThread(Thread currentThread) =>
        !OSVersionInfo.IsWin || currentThread.GetApartmentState() == ApartmentState.STA;
}