using FEx.Common.Startup;
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
    private readonly Host _host;

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
        _host = new Host(this);

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

    /// <summary>
    /// Handles a startup failure before the service container exists (a rejected configuration such as
    /// <see cref="Application.StartupUri" />, or a failed container build), when <see cref="HandleException" /> cannot
    /// rely on the container's exception handling. Writes to the trace output and shows a message box.
    /// </summary>
    protected virtual void HandleEarlyException(Exception exception)
    {
        Trace.TraceError(exception.ToString());
        MessageBox.Show(exception.Message, "Startup failed", MessageBoxButton.OK, MessageBoxImage.Error);
    }

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
    /// Creates the main window once the container is built and all hooks ran; the base assigns it to
    /// <see cref="Application.MainWindow" /> and shows it. Return <c>null</c> if the app shows its windows itself.
    /// </summary>
    /// <remarks>
    /// <see cref="Application.StartupUri" /> is not supported: WPF would load it as soon as <see cref="OnStartup" />
    /// first yields, before the container exists. Remove it from App.xaml and return the window from here.
    /// </remarks>
    protected virtual Window? CreateMainWindow() => null;

    /// <summary>
    /// Starts the app: shows the optional startup window, awaits the container build without blocking the
    /// dispatcher, then runs the startup hooks in order. A failure goes through <see cref="HandleException" /> and
    /// exits with a non-zero code.
    /// </summary>
    /// <param name="e">A <see cref="StartupEventArgs" /> that contains the event data.</param>
#pragma warning disable VSTHRD100 // async void is the only way to await inside the Application.OnStartup override; the host catches everything.
    protected sealed override async void OnStartup(StartupEventArgs e)
    {
        _host.StartupArgs = e;
        await _host.RunAsync();
    }
#pragma warning restore VSTHRD100

    private void RaiseStartup(StartupEventArgs e) => base.OnStartup(e);

    /// <summary>Forwards the shared <see cref="DesktopStartupHost{TWindow,TShutdownMode}" /> primitives to the WPF <see cref="Application" />.</summary>
    private sealed class Host(AppBootstrapper<TContainer> app) : DesktopStartupHost<Window, ShutdownMode>
    {
        internal StartupEventArgs StartupArgs { get; set; } = null!;

        protected override ShutdownMode ShutdownMode
        {
            get => app.ShutdownMode;
            set => app.ShutdownMode = value;
        }

        protected override ShutdownMode ExplicitShutdownMode => ShutdownMode.OnExplicitShutdown;

        protected override Window? MainWindow
        {
            get => app.MainWindow;
            set => app.MainWindow = value;
        }

        protected override bool HasStartupUri => app.StartupUri is not null;

        protected override Window? CreateStartupWindow() => app.CreateStartupWindow();

#pragma warning disable IDISP004 // intentional _=LogToHub pattern for scoped status logging
        protected override Window? CreateMainWindow()
        {
            _ = app.LogToHub("Showing window");
            return app.CreateMainWindow();
        }
#pragma warning restore IDISP004

        protected override void Show(Window window) => window.Show();

        protected override void Close(Window window) => window.Close();

        protected override void OnMainWindowShown() => app.RaiseStartup(StartupArgs);

        protected override async Task InitializeContainerAsync() =>
            app._container = await FExServiceProvider.InitializeAsync<TContainer>();

        protected override void SetMainThread() => FExCoreStatics.MainThreadContextProvider.SetMainThread();

        protected override void PublishServices()
        {
            app._appInfoProvider = FExServiceProvider.Get<IAppInfoProvider>();
            app._appConfig = FExServiceProvider.Get<IAppConfig>();
            app._exceptionHandler = FExServiceProvider.Get<IExceptionHandler>();
            app._exceptionHandler.ExceptionOccured += (_, _) => app.ExitApp();
            app._statusService = FExServiceProvider.Get<IStatusService>();
        }

        protected override void OnActivation() => app.OnActivation();

        protected override void EnsureSingleInstance() => app.EnsureSingleInstance();

#pragma warning disable IDISP004 // intentional using(_=LogToHub) pattern for scoped status logging
        protected override void InitializeComponents()
        {
            using (_ = app.LogToHub("Initializing app"))
            {
                app.OnConstruction(StartupArgs);
                app.BeforeInitializationCheck();
                app.ComponentInitialize();
            }
        }

        protected override void BeforeStartup()
        {
            using (_ = app.LogToHub("Preparing app"))
                app.BeforeStartup(StartupArgs);
        }

        protected override void AfterServicesContainerBuild()
        {
            using (_ = app.LogToHub("Initializing app components"))
                app.AfterServicesContainerBuild();
        }

        protected override void AfterStartup()
        {
            using (_ = app.LogToHub("Finalizing startup"))
                app.AfterStartup(StartupArgs);
        }
#pragma warning restore IDISP004

        protected override void ExitIfInitializationHasFailed() => app.ExitIfInitializationHasFailed();

        protected override void HandleException(Exception exception) => app.HandleException(exception);

        protected override void HandleEarlyException(Exception exception) => app.HandleEarlyException(exception);

        protected override void RequestExit(int exitCode) => app.ExitApp(exitCode);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        FExMvvm.MessagePopupService.AppIsClosing = true;
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    protected DisposableAction LogToHub(string status) => StatusService.Log(status);
}