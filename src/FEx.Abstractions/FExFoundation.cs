using FEx.Abstractions.Implementations;
using FEx.Abstractions.Interfaces;
using FEx.Common.Abstractions.Interfaces;
using FEx.Common.Comparers;
using FEx.Common.Extensions;
using FEx.Common.Providers;
using System;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Abstractions;

public class FExFoundation : FExInitialize, IFExPriorityInitialize
{
    private static readonly MainThreadContextProvider DefaultMainThreadContextProvider;
    private static readonly DebugExceptionHandler DefaultExceptionHandler;
    private static readonly DefaultStackTraceProvider DefaultStackTraceProvider;
    private static readonly AlphanumComparatorFast DefaultAlphanumComparatorFast;

    private static Func<IStackTraceProvider> _stackTraceProviderFactory;
    private static Func<IFExDispatcher> _dispatcherFactory;
    private static Func<IAsyncHelper> _asyncHelperFactory;
    private static Func<IExceptionHandler> _exceptionHandlerFactory;
    private static Func<ISynchronizedAccessService> _synchronizedAccessServiceFactory;
    private static Func<IAppInfoProvider> _appInfoProviderFactory;
    private static Func<AlphanumComparatorFast> _alphanumComparatorFastFactory;
    private static Func<IMainThreadContextProvider> _mainThreadContextProviderFactory;
    private static FExFoundation _instance;

    public static bool HasBeenInitialized => _instance.IsInitialized;

    [Obsolete("Discouraged - use only where DI is unavailable")]
    public static IStackTraceProvider StackTraceProvider =>
        (_stackTraceProviderFactory is not null
            ? _stackTraceProviderFactory()
            : ServiceProvider?.GetRequiredService<IStackTraceProvider>())
        ?? DefaultStackTraceProvider;

    [Obsolete("Discouraged - use only where DI is unavailable")]
    public static IFExDispatcher Dispatcher =>
        (_dispatcherFactory is not null
            ? _dispatcherFactory()
            : ServiceProvider.GetRequiredService<IFExDispatcher>()).Guard();

    [Obsolete("Discouraged - use only where DI is unavailable")]
    public static IAsyncHelper AsyncHelper =>
        (_asyncHelperFactory is not null
            ? _asyncHelperFactory()
            : ServiceProvider.GetRequiredService<IAsyncHelper>()).Guard();

    [Obsolete("Discouraged - use only where DI is unavailable")]
    public static IExceptionHandler ExceptionHandler =>
        (_exceptionHandlerFactory is not null
            ? _exceptionHandlerFactory()
            : ServiceProvider?.GetRequiredService<IExceptionHandler>())
        ?? DefaultExceptionHandler;

    [Obsolete("Discouraged - use only where DI is unavailable")]
    public static ISynchronizedAccessService SynchronizedAccessService =>
        (_synchronizedAccessServiceFactory is not null
            ? _synchronizedAccessServiceFactory()
            : ServiceProvider.GetRequiredService<ISynchronizedAccessService>()).Guard();

    [Obsolete("Discouraged - use only where DI is unavailable")]
    public static IAppInfoProvider AppInfoProvider =>
        (_appInfoProviderFactory is not null
            ? _appInfoProviderFactory()
            : ServiceProvider.GetRequiredService<IAppInfoProvider>()).Guard();

    [Obsolete("Discouraged - use only where DI is unavailable")]
    public static AlphanumComparatorFast AlphanumComparatorFast =>
        (_alphanumComparatorFastFactory is not null
            ? _alphanumComparatorFastFactory()
            : ServiceProvider?.GetRequiredService<AlphanumComparatorFast>() ?? DefaultAlphanumComparatorFast).Guard();

    [Obsolete("Discouraged - use only where DI is unavailable")]
    public static IMainThreadContextProvider MainThreadContextProvider =>
        (_mainThreadContextProviderFactory is not null
            ? _mainThreadContextProviderFactory()
            : ServiceProvider?.GetRequiredService<IMainThreadContextProvider>())
        ?? DefaultMainThreadContextProvider;

    [Obsolete($"Discouraged - use {nameof(IAppInfoProvider)} where DI is available")]
    public static bool IsUIApp => AppInfoProvider?.AppInfo is { IsUIApp: true };

    /// <inheritdoc />
    public int Priority { get; }

    private static IFExServiceProvider ServiceProvider { get; set; }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public FExFoundation(IFExServiceProvider serviceProvider,
                         IStackTraceProvider stackTraceProvider,
                         IFExDispatcher dispatcher,
                         IAsyncHelper asyncHelper,
                         IExceptionHandler exceptionHandler,
                         ISynchronizedAccessService synchronizedAccessService,
                         IAppInfoProvider appInfoProvider,
                         AlphanumComparatorFast alphanumComparatorFast,
                         IMainThreadContextProvider mainThreadContextProvider)
    {
        ServiceProvider = serviceProvider;
        Priority = -1;
        _instance = this;
    }

    static FExFoundation()
    {
        DefaultStackTraceProvider = new();
        DefaultExceptionHandler = new();
        DefaultMainThreadContextProvider = new(null);
#pragma warning disable CS0618 // Type or member is obsolete
        MainThreadContextProvider.SetMainThread(false);
#pragma warning restore CS0618 // Type or member is obsolete
        DefaultAlphanumComparatorFast = new();
    }

    public static void Initialize(Func<IStackTraceProvider> stackTraceProviderFactory,
                                  Func<IFExDispatcher> dispatcherFactory,
                                  Func<IAsyncHelper> asyncHelperFactory,
                                  Func<IExceptionHandler> exceptionHandlerFactory,
                                  Func<ISynchronizedAccessService> synchronizedAccessServiceFactory,
                                  Func<IAppInfoProvider> appInfoProviderFactory,
                                  Func<AlphanumComparatorFast> alphanumComparatorFastFactory,
                                  Func<IMainThreadContextProvider> mainThreadContextProviderFactory)
    {
        if (stackTraceProviderFactory is not null)
            _stackTraceProviderFactory = stackTraceProviderFactory;

        if (dispatcherFactory is not null)
            _dispatcherFactory = dispatcherFactory;

        if (asyncHelperFactory is not null)
            _asyncHelperFactory = asyncHelperFactory;

        if (exceptionHandlerFactory is not null)
            _exceptionHandlerFactory = exceptionHandlerFactory;

        if (synchronizedAccessServiceFactory is not null)
            _synchronizedAccessServiceFactory = synchronizedAccessServiceFactory;

        if (appInfoProviderFactory is not null)
            _appInfoProviderFactory = appInfoProviderFactory;

        if (alphanumComparatorFastFactory is not null)
            _alphanumComparatorFastFactory = alphanumComparatorFastFactory;

        if (mainThreadContextProviderFactory is not null)
            _mainThreadContextProviderFactory = mainThreadContextProviderFactory;

        new FExFoundation(null, null, null, null, null, null, null, null, null).Initialize();
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
#pragma warning disable CS0618 // Type or member is obsolete
        MainThreadContextProvider.SetMainThread();
#pragma warning restore CS0618 // Type or member is obsolete
    }
}