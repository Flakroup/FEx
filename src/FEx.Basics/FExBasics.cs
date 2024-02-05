using FEx.Abstractions.Interfaces;
using FEx.Basics.Abstractions.Interfaces;
using FEx.Basics.Utilities;
using FEx.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace FEx.Basics;

public class FExBasics
{
    private static Thread _mainThread;
    private static SynchronizationContext _mainSynchronizationContext;

    public static IStackTraceProvider StackTraceProvider { get; private set; }
    public static IEventDeliverer EventDeliverer { get; private set; }
    public static ILogger Logger { get; private set; }
    public static ISynchronizedAccessService SynchronizedAccessService { get; private set; }
    public static IAppInfoProvider AppInfoProvider { get; private set; }

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

    public static bool IsDispatcherContext { get; set; }
    public static bool SendEventsInCreationContext { get; set; }
    public static bool IsUIApp => Utilities.AppInfoProvider.IsUIApp;

    static FExBasics()
    {
        StackTraceProvider = new DefaultStackTraceProvider();
    }

    public static void Init(IStackTraceProvider stackTraceProvider,
                            IEventDeliverer eventDeliverer,
                            ILogger logger,
                            ISynchronizedAccessService synchronizedAccessService,
                            IAppInfoProvider appInfoProvider)
    {
        AppInfoProvider = appInfoProvider.Guard(nameof(appInfoProvider));
        StackTraceProvider = stackTraceProvider.Guard(nameof(stackTraceProvider));
        EventDeliverer = eventDeliverer.Guard(nameof(eventDeliverer));
        Logger = logger.Guard(nameof(logger));
        SynchronizedAccessService = synchronizedAccessService.Guard(nameof(synchronizedAccessService));

        SetMainThread();
    }

    public static void SetMainThread()
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
            _ = MainThread.GetThreadSynchronizationContext(true);
    }

    private static bool IsPlatformMainThread(Thread currentThread) =>
        !PlatformInfoProvider.IsWindows || !IsUIApp || currentThread.GetApartmentState() == ApartmentState.STA;
}