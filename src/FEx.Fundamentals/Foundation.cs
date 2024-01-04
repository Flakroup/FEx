using FEx.Abstractions;
using FEx.DependencyInjection;
using FEx.Extensions;
using FEx.Fundamentals.Utilities;
using System;
using System.Threading;

namespace FEx.Fundamentals;

public class Foundation
{
    private static IFExDispatcher _dispatcher;
    private static IAppInfoProvider _appInfoProvider;
    private static IFExServiceProvider _serviceProvider;
    private static FExStrongInjectServiceProvider _strongInjectServiceProvider;
    private static Thread _mainThread;
    private static SynchronizationContext _mainSynchronizationContext;

    public static IFExDispatcher Dispatcher
    {
        get => _dispatcher;
        private set => _dispatcher = value.Guard();
    }

    public static IAppInfoProvider AppInfoProvider
    {
        get => _appInfoProvider;
        private set => _appInfoProvider = value.Guard();
    }

    public static IFExServiceProvider ServiceProvider
    {
        get => _serviceProvider.Guard();
        private set => _serviceProvider = value;
    }

    public static FExStrongInjectServiceProvider StrongInjectServiceProvider
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

    public static bool IsUIApp { get; private set; }

    private static bool IsDispatcherContext { get; set; }

    public Foundation(IFExDispatcher dispatcher,
                      IAppInfoProvider appInfoProvider)
    {
        Dispatcher = dispatcher;
        AppInfoProvider = appInfoProvider;
    }

    public static void Init<TContainer>(bool isUIApp = false, IFExServiceProvider microsoftDiServiceProvider = null)
        where TContainer : class, IDisposable, new()
    {
        IsUIApp = isUIApp;
        SetMainThread();
        _strongInjectServiceProvider?.Dispose();
        StrongInjectServiceProvider = new FExStrongInjectServiceProvider();
        StrongInjectServiceProvider.ConfigureServiceProvider<TContainer>();
        ServiceProvider = microsoftDiServiceProvider ?? StrongInjectServiceProvider;
        IsInitialized = true;
    }

    public static void RegisterDependencies(Func<IFExServiceProvider> serviceProviderConfiguration)
    {
        serviceProviderConfiguration.Guard(nameof(serviceProviderConfiguration));
        ServiceProvider = serviceProviderConfiguration();
    }

    private static void SetMainThread()
    {
        Thread currentThread = Thread.CurrentThread;

        bool isMainThread = IsPlatformMainThread(currentThread)
                            && !currentThread.IsBackground
                            && currentThread.IsAlive
                            && !currentThread.IsThreadPoolThread;

        if (!isMainThread)
            throw new InvalidOperationException("This method must be called from main thread.");

        MainThread = currentThread;

        if (IsUIApp)
            MainThread.GetThreadSynchronizationContext(true);
    }

    private static bool IsPlatformMainThread(Thread currentThread) =>
        !PlatformInfoProvider.IsWindows || !IsUIApp || currentThread.GetApartmentState() == ApartmentState.STA;
}