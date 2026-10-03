using FEx.AppStartup;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Common.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using FEx.Core.Abstractions.Utilities;
using FEx.DependencyInjection.Abstractions;
using FEx.MVVM;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Extensions;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.WpfBindingErrors;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
#if !NET5_0_OR_GREATER
using System.Net;
#endif
using System.Windows;
using System.Windows.Threading;

namespace FEx.WPFx.Abstractions;

public abstract class AppBootstrapper<TContainer> : Application
    where TContainer : class, IFExContainer, IDisposable, new()
{
    private TContainer? _container;
    private IAppInfoProvider? _appInfoProvider;
    private IExceptionHandler? _exceptionHandler;
    private IStatusService? _statusService;
    private IAppConfig? _appConfig;
    private Window? _startupWindow;
    private ShutdownMode _configuredShutdownMode;

    // The services below are resolved by the startup flow in OnStartup and are valid only after the container was
    // built, i.e. from OnActivation onwards. Reading them earlier (for example in a subclass constructor) throws.
    protected TContainer Container => Ready(_container);
    protected IAppInfoProvider AppInfoProvider => Ready(_appInfoProvider);
    protected IExceptionHandler ExceptionHandler => Ready(_exceptionHandler);
    protected IStatusService StatusService => Ready(_statusService);
    protected IAppConfig AppConfig => Ready(_appConfig);

    protected DirectoryInfo AppData => AppInfoProvider.AppData;
    protected DirectoryInfo UserData => AppInfoProvider.UserData;
    protected string UserSettingsPath => AppInfoProvider.UserSettingsPath;
    protected string ApplicationName => AppInfoProvider.Name;

    protected AppBootstrapper()
    {
        AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
        DispatcherUnhandledException += OnAppDispatcherUnhandledException;

        SetNetwork();
    }

    private static T Ready<T>(T? service) where T : class =>
        service ?? throw new InvalidOperationException(
            "The service container is built asynchronously during OnStartup; this service is available from OnActivation onwards.");

    protected abstract void ComponentInitialize();
    protected abstract void OnActivation();

    protected virtual void AfterServicesContainerBuild()
    {
    }

    protected virtual void SetNetwork()
    {
        // SYSLIB0014: ServicePointManager settings are obsolete no-ops on net5+ (they do not
        // affect HttpClient); they still tune TLS and Nagle on .NET Framework / netstandard, so
        // they are compiled only there (the using System.Net is guarded by the same condition).
#if !NET5_0_OR_GREATER
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                               | SecurityProtocolType.Tls11
                                               | SecurityProtocolType.Tls12
                                               | SecurityProtocolType.Tls13;

        ServicePointManager.UseNagleAlgorithm = false;
#endif
    }

    protected virtual void HandleException(Exception exception) => exception.HandleException(true, true);

    protected virtual void BeforeStartup(StartupEventArgs e)
    {
        FExWpfx.OverrideFormattingOnUI();

        AppConfig.Initialize();

        ExitIfInitializationHasFailed();
    }

    protected virtual void ExitIfInitializationHasFailed() => ExitIfInitializationHasFailed(1);

    protected virtual void ExitIfInitializationHasFailed(int exitCode)
    {
        if (ExceptionHandler.LastException is null)
            return;

        ExitApp(exitCode);
    }

    protected virtual void ExitApp() => ExitApp(1);

    protected virtual void ExitApp(int exitCode) => Environment.Exit(exitCode);

    protected virtual bool HasInitializationFailed() => ExceptionHandler.LastException is not null;

    protected virtual void AfterStartup(StartupEventArgs e) =>
        BindingExceptionThrower.Attach(_appInfoProvider?.AppData.FullName);

    protected virtual void OnConstruction(StartupEventArgs e)
    {
    }

    protected virtual void OnAppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Exception is not null)
            HandleException(e.Exception);

        e.Handled = true;
    }

    protected virtual void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception)
            return;

        HandleException(exception);
    }

    protected virtual void BeforeInitializationCheck()
    {
    }

    protected virtual void EnsureSingleInstance()
    {
#pragma warning disable IDISP004 // intentional using(_=LogToHub) pattern for scoped status logging
        using (_ = LogToHub("Checking duplicated instances"))
#pragma warning restore IDISP004
        {
            var otherInstances = AppUtility.GetOtherInstances();
            var isSingleInstance = otherInstances.Length == 0;

            if (isSingleInstance)
                return;

            if (FExMvvm.MessagePopupService.ShowMessage(
                    $"{_appInfoProvider?.Name ?? "App"} is already running.{Environment.NewLine}Do you want to close it?",
                    "Duplicated instance",
                    MessageIcon.Exclamation,
                    FExMessageButton.YesNo)
                == MessageResult.No)
                ExitApp(0);

            foreach (var pid in otherInstances)
            {
                using var p = Process.GetProcessById(pid);
                p.Kill();
            }
        }
    }

    /// <summary>
    /// Optional window shown while the service container is built, so the app can paint during startup.
    /// It is closed as soon as the container is ready; the app does not shut down when it closes.
    /// Runs before the container exists, so it must not use any container service.
    /// </summary>
    protected virtual Window? CreateStartupWindow() => null;

    /// <summary>
    /// Starts the app: shows the optional startup window, awaits the container build without blocking the
    /// dispatcher, then runs the startup hooks in order. A failure goes through <see cref="HandleException" /> and
    /// exits with a non-zero code.
    /// </summary>
    /// <param name="e">A <see cref="StartupEventArgs" /> that contains the event data.</param>
#pragma warning disable VSTHRD100 // async void is the only way to await inside the Application.OnStartup override; the sequencer catches everything.
    protected sealed override async void OnStartup(StartupEventArgs e) =>
        await StartupSequencer.RunAsync(ShowStartupWindow,
                                        InitializeContainerAsync,
                                        CloseStartupWindow,
                                        GetStartupHooks(e),
                                        HandleException,
                                        ExitApp);
#pragma warning restore VSTHRD100

    private async Task InitializeContainerAsync()
    {
        FExCoreStatics.MainThreadContextProvider.SetMainThread();
        _container = await FExServiceProvider.InitializeAsync<TContainer>();
    }

    // The startup window is the only window while the container builds; keep the app alive when it closes and give
    // it back to the app's own setting afterwards.
    private void ShowStartupWindow()
    {
        _configuredShutdownMode = ShutdownMode;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _startupWindow = CreateStartupWindow();
        _startupWindow?.Show();
    }

    private void CloseStartupWindow()
    {
        if (_startupWindow is null)
            return;

        _startupWindow.Close();

        // The first window created becomes MainWindow; the startup window must not.
        if (ReferenceEquals(MainWindow, _startupWindow))
            MainWindow = null;

        _startupWindow = null;
    }

    private Action[] GetStartupHooks(StartupEventArgs e) =>
    [
        PublishServices,
        OnActivation,
        EnsureSingleInstance,
#pragma warning disable IDISP004 // intentional using(_=LogToHub) pattern for scoped status logging
        () =>
        {
            using (_ = LogToHub("Initializing app"))
            {
                OnConstruction(e);
                BeforeInitializationCheck();
                ComponentInitialize();
            }
        },
        () =>
        {
            using (_ = LogToHub("Preparing app"))
                BeforeStartup(e);
        },
        () =>
        {
            using (_ = LogToHub("Initializing app components"))
                AfterServicesContainerBuild();
        },
        () =>
        {
            _ = LogToHub("Showing window");
            base.OnStartup(e);
            ShutdownMode = _configuredShutdownMode;
        },
        () =>
        {
            using (_ = LogToHub("Finalizing startup"))
                AfterStartup(e);
        },
#pragma warning restore IDISP004
        ExitIfInitializationHasFailed
    ];

    private void PublishServices()
    {
        _appInfoProvider = FExServiceProvider.Get<IAppInfoProvider>();
        _appConfig = FExServiceProvider.Get<IAppConfig>();
        _exceptionHandler = FExServiceProvider.Get<IExceptionHandler>();
        _exceptionHandler.ExceptionOccured += (_, _) => ExitApp();
        _statusService = FExServiceProvider.Get<IStatusService>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        FExMvvm.MessagePopupService.AppIsClosing = true;
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    protected DisposableAction LogToHub(string status) => StatusService.Log(status);
}