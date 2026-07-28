using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Comparers;
using FEx.Core.Abstractions.Helpers;
using FEx.Core.Abstractions.Implementations;
using FEx.Core.Abstractions.Interfaces;
using FEx.Core.Abstractions.Providers;
using FEx.Core.Abstractions.Services;
using FEx.Core.Abstractions.Settings;
using FEx.DependencyInjection.Abstractions.Basics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Reflection;

namespace FEx.Core.Abstractions;

public class FExCoreStatics : StaticsBase
{
    private static readonly StackTraceProvider DefaultStackTraceProviderInstance;
    private static readonly ILogger DefaultLoggerInstance;
    private static readonly DebugExceptionHandler DefaultExceptionHandler;
    private static readonly IAppInfoProvider DefaultAppInfoProvider;
    private static readonly MainThreadContextProvider DefaultMainThreadContextProvider;
    private static readonly DeadlockMonitor DefaultDeadlockMonitor;
    private static readonly DefaultDispatcher DefaultDispatcherInstance;
    private static readonly AsyncHelper DefaultAsyncHelperInstance;
    private static readonly SynchronizedAccessService DefaultSynchronizedAccessServiceInstance;
    private static readonly AlphanumComparatorFast DefaultAlphanumComparatorFastInstance;

    private static Func<IAsyncHelper>? _asyncHelperFactory;
    private static Func<IStackTraceProvider>? _stackTraceProviderFactory;
    private static Func<IFExDispatcher>? _dispatcherFactory;
    private static Func<IExceptionHandler>? _exceptionHandlerFactory;
    private static Func<ILogger>? _loggerFactory;
    private static Func<IMainThreadContextProvider>? _mainThreadContextProviderFactory;
    private static Func<IDeadlockMonitor>? _deadlockMonitorFactory;
    private static Func<IAppInfoProvider>? _appInfoProviderFactory;
    private static Func<ISynchronizedAccessService>? _synchronizedAccessServiceFactory;
    private static Func<AlphanumComparatorFast>? _alphanumComparatorFastFactory;

    /// <summary>
    /// Retrieves the <see cref="IAsyncHelper" /> instance.
    /// <br />
    /// ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static IAsyncHelper AsyncHelper => Get(_asyncHelperFactory, static () => DefaultAsyncHelperInstance);

    /// <summary>
    /// Retrieves the <see cref="IStackTraceProvider" /> instance.
    /// <br />
    /// ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static IStackTraceProvider StackTraceProvider =>
        Get(_stackTraceProviderFactory, static () => DefaultStackTraceProviderInstance);

    /// <summary>
    /// Retrieves the <see cref="ILogger" /> instance.
    /// <br />
    /// ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static ILogger Logger => Get(_loggerFactory, static () => DefaultLoggerInstance);

    /// <summary>
    /// Retrieves the <see cref="IDeadlockMonitor" /> instance.
    /// <br />
    /// ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static IDeadlockMonitor DeadlockMonitor =>
        Get(_deadlockMonitorFactory, static () => DefaultDeadlockMonitor);

    /// <summary>
    /// Retrieves the <see cref="IFExDispatcher" /> instance.
    /// <br />
    /// ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static IFExDispatcher Dispatcher => Get(_dispatcherFactory, static () => DefaultDispatcherInstance);

    /// <summary>
    /// Retrieves the <see cref="IExceptionHandler" /> instance.
    /// <br />
    /// ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static IExceptionHandler ExceptionHandler =>
        Get(_exceptionHandlerFactory, static () => DefaultExceptionHandler);

    public static IMainThreadContextProvider MainThreadContextProvider =>
        Get(_mainThreadContextProviderFactory, static () => DefaultMainThreadContextProvider);

    public static IAppInfoProvider AppInfoProvider =>
        Get(_appInfoProviderFactory, static () => DefaultAppInfoProvider);

    /// <summary>
    /// Retrieves the <see cref="ISynchronizedAccessService" /> instance.
    /// <br />
    /// ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static ISynchronizedAccessService SynchronizedAccessService =>
        Get(_synchronizedAccessServiceFactory, static () => DefaultSynchronizedAccessServiceInstance);

    public static AlphanumComparatorFast AlphanumComparatorFast =>
        Get(_alphanumComparatorFastFactory, static () => DefaultAlphanumComparatorFastInstance);

    static FExCoreStatics()
    {
        // Create basic default instances that do not rely on DI.
        DefaultStackTraceProviderInstance = new();
        DefaultLoggerInstance = NullLogger.Instance;
        DefaultExceptionHandler = new();
        DefaultAppInfoProvider = new NullAppInfoProvider();
        DefaultMainThreadContextProvider = new(DefaultAppInfoProvider);
        DefaultDeadlockMonitor = new(DefaultStackTraceProviderInstance, DefaultLoggerInstance);
        var defaultAppThreadingSettings = new AppThreadingSettings();

        DefaultDispatcherInstance = new(DefaultLoggerInstance,
            DefaultMainThreadContextProvider,
            DefaultDeadlockMonitor,
            DefaultStackTraceProviderInstance,
            defaultAppThreadingSettings);

        DefaultAsyncHelperInstance = new(DefaultDispatcherInstance, DefaultExceptionHandler);
        DefaultSynchronizedAccessServiceInstance = new();
        DefaultAlphanumComparatorFastInstance = new();

        FExAgnosticsStatics.Configure(DefaultAsyncHelperInstance);
    }

    public static void SetDefaults()
    {
        _stackTraceProviderFactory = null;
        _dispatcherFactory = null;
        _asyncHelperFactory = null;
        _exceptionHandlerFactory = null;
        _loggerFactory = null;
        _mainThreadContextProviderFactory = null;
        _deadlockMonitorFactory = null;
        _appInfoProviderFactory = null;
        _synchronizedAccessServiceFactory = null;
        _alphanumComparatorFastFactory = null;
    }

#pragma warning disable S2360 // Optional parameters should not be used - Configure uses named arguments pattern, overloads impractical for 10 independent params
    public static void Configure(Func<IStackTraceProvider>? stackTraceProviderFactory = null,
                                 Func<IFExDispatcher>? dispatcherFactory = null,
                                 Func<IAsyncHelper>? asyncHelperFactory = null,
                                 Func<ILogger>? loggerFactory = null,
                                 Func<IMainThreadContextProvider>? mainThreadContextProviderFactory = null,
                                 Func<IDeadlockMonitor>? deadlockMonitorFactory = null,
                                 Func<IAppInfoProvider>? appInfoProviderFactory = null,
                                 Func<IExceptionHandler>? exceptionHandlerFactory = null,
                                 Func<ISynchronizedAccessService>? synchronizedAccessServiceFactory = null,
                                 Func<AlphanumComparatorFast>? alphanumComparatorFastFactory = null)
    {
        if (stackTraceProviderFactory is not null)
            _stackTraceProviderFactory = stackTraceProviderFactory;

        if (dispatcherFactory is not null)
            _dispatcherFactory = dispatcherFactory;

        if (asyncHelperFactory is not null)
        {
            _asyncHelperFactory = asyncHelperFactory;
            FExAgnosticsStatics.Configure(asyncHelperFactory());
        }

        if (loggerFactory is not null)
            _loggerFactory = loggerFactory;

        if (mainThreadContextProviderFactory is not null)
            _mainThreadContextProviderFactory = mainThreadContextProviderFactory;

        if (deadlockMonitorFactory is not null)
            _deadlockMonitorFactory = deadlockMonitorFactory;

        if (appInfoProviderFactory is not null)
            _appInfoProviderFactory = appInfoProviderFactory;

        if (exceptionHandlerFactory is not null)
            _exceptionHandlerFactory = exceptionHandlerFactory;

        if (synchronizedAccessServiceFactory is not null)
            _synchronizedAccessServiceFactory = synchronizedAccessServiceFactory;

        if (alphanumComparatorFastFactory is not null)
            _alphanumComparatorFastFactory = alphanumComparatorFastFactory;
    }
#pragma warning restore S2360

    #region Null/default helper types
    // Null-object providers: every member intentionally returns null under a non-null interface
    // contract (used as a no-op fallback where DI is unavailable). null! keeps that runtime behavior.
    private sealed class NullAppInfoProvider : IAppInfoProvider
    {
        public string EntryAssemblyName => null!;
        public Assembly EntryAssembly => null!;
        public FileInfo EntryAssemblyLocation => null!;
        public string Name => null!;
        public Version Version => null!;
        public string VersionString => null!;
        public string Company => null!;
        public string Copyright => null!;
        public string NameAndVersionWithPrefix => null!;
        public string NameLineVersion => null!;
        public string NameLineVersionWithPrefix => null!;
        public string NameAndVersion => null!;
        public DirectoryInfo UserData => null!;
        public string UserDataPath => null!;
        public DirectoryInfo AppData => null!;
        public string AppDataPath => null!;
        public string UserSettingsPath => null!;
        public IAppInfo AppInfo { get; } = new NullAppInfo();

        private sealed class NullAppInfo : IAppInfo
        {
            public string Name => null!;
            public Version Version => null!;
            public string Company => null!;
            public bool IsUIApp { get; set; } = false;
            public DirectoryInfo UserData => null!;
            public DirectoryInfo AppData => null!;
            public string LogDirPath => null!;
        }
    }
    #endregion
}