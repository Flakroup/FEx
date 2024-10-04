using FEx.Abstractions.Implementations;
using FEx.Abstractions.Interfaces;
using FEx.Common.Extensions;
using FEx.Common.Utilities;
using System;
using System.Threading;

namespace FEx.Abstractions;

public class FExFoundation : IFExInitialize
{
    private static IStackTraceProvider _stackTraceProvider;
    private static IFExDispatcher _dispatcher;
    private static IAsyncHelper _asyncHelper;
    private static IExceptionHandler _exceptionHandler;
    private static Thread _mainThread;
    private static SynchronizationContext _mainSynchronizationContext;
    private static IEventDeliverer _eventDeliverer;
    private static ISynchronizedAccessService _synchronizedAccessService;
    private static IAppInfoProvider _appInfoProvider;

    public static IStackTraceProvider StackTraceProvider
    {
        get => _stackTraceProvider.Guard(nameof(StackTraceProvider));
        private set => _stackTraceProvider = value.Guard(nameof(value));
    }

    public static IFExDispatcher Dispatcher
    {
        get => _dispatcher.Guard(nameof(Dispatcher));
        private set => _dispatcher = value.Guard(nameof(Dispatcher));
    }

    public static IAsyncHelper AsyncHelper
    {
        get => _asyncHelper.Guard(nameof(AsyncHelper));
        private set => _asyncHelper = value.Guard(nameof(value));
    }

    public static IExceptionHandler ExceptionHandler
    {
        get => _exceptionHandler.Guard(nameof(ExceptionHandler));
        private set => _exceptionHandler = value.Guard(nameof(value));
    }

    public static IEventDeliverer EventDeliverer
    {
        get => _eventDeliverer.Guard(nameof(EventDeliverer));
        private set => _eventDeliverer = value.Guard(nameof(value));
    }

    public static ISynchronizedAccessService SynchronizedAccessService
    {
        get => _synchronizedAccessService.Guard(nameof(SynchronizedAccessService));
        private set => _synchronizedAccessService = value.Guard(nameof(value));
    }

    public static IAppInfoProvider AppInfoProvider
    {
        get => _appInfoProvider.Guard(nameof(AppInfoProvider));
        private set => _appInfoProvider = value.Guard(nameof(value));
    }

    public static Thread MainThread
    {
        get => _mainThread.Guard(nameof(MainThread));
        private set => _mainThread = value.Guard(nameof(value));
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
    public static bool IsUIApp => AppInfoProvider.AppInfo is { IsUIApp: true };

    public FExFoundation(IStackTraceProvider stackTraceProvider,
                         IFExDispatcher dispatcher,
                         IAsyncHelper asyncHelper,
                         IExceptionHandler exceptionHandler)
    {
        StackTraceProvider = stackTraceProvider;
        Dispatcher = dispatcher;
        AsyncHelper = asyncHelper;
        ExceptionHandler = exceptionHandler;

        SetMainThread();
    }

    static FExFoundation()
    {
        StackTraceProvider = new DefaultStackTraceProvider();
        ExceptionHandler = new DebugExceptionHandler();
    }

    /// <summary>
    /// This is to be called by ServiceProvider
    /// </summary>
    public void Initialize()
    {
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

    public virtual void Init(IStackTraceProvider stackTraceProvider,
                             IFExDispatcher dispatcher,
                             IAsyncHelper asyncHelper,
                             IExceptionHandler exceptionHandler)
    {
        StackTraceProvider = stackTraceProvider;
        Dispatcher = dispatcher;
        AsyncHelper = asyncHelper;
        ExceptionHandler = exceptionHandler;
    }

    private static bool IsPlatformMainThread(Thread currentThread) =>
        !PlatformInfoProvider.IsWindows || !IsUIApp || currentThread.GetApartmentState() == ApartmentState.STA;
}