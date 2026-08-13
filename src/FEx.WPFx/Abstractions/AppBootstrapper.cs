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
#if !NET5_0_OR_GREATER
using System.Net;
#endif
using System.Windows;
using System.Windows.Threading;

namespace FEx.WPFx.Abstractions;

public abstract class AppBootstrapper<TContainer> : Application
    where TContainer : class, IFExContainer, IDisposable, new()
{
    // Resolved from the container in the constructor's try block; non-null once construction succeeds.
    protected readonly IAppInfoProvider _appInfoProvider = null!;
    protected readonly IExceptionHandler _exceptionHandler = null!;
    protected readonly IStatusService _statusService = null!;
    protected readonly IAppConfig _appConfig = null!;
    protected readonly TContainer _container = null!;

    protected DirectoryInfo AppData => _appInfoProvider.AppData;
    protected DirectoryInfo UserData => _appInfoProvider.UserData;
    protected string UserSettingsPath => _appInfoProvider.UserSettingsPath;
    protected string ApplicationName => _appInfoProvider.Name;

    protected AppBootstrapper()
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
            DispatcherUnhandledException += OnAppDispatcherUnhandledException;

            SetNetwork();

            // VSTHRD002: synchronous wait is required at this WPF bootstrap entry point; exceptions
            // are handled by the surrounding try/catch.
#pragma warning disable VSTHRD002
            _container = FExServiceProvider.InitializeAsync<TContainer>().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
            _appInfoProvider = FExServiceProvider.Get<IAppInfoProvider>();
            _appConfig = FExServiceProvider.Get<IAppConfig>();
            _exceptionHandler = FExServiceProvider.Get<IExceptionHandler>();
            _exceptionHandler.ExceptionOccured += (_, _) => ExitApp();
            _statusService = FExServiceProvider.Get<IStatusService>();
            OnActivation();
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

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

        _appConfig.Initialize();

        ExitIfInitializationHasFailed();
    }

    protected virtual void ExitIfInitializationHasFailed() => ExitIfInitializationHasFailed(1);

    protected virtual void ExitIfInitializationHasFailed(int exitCode)
    {
        if (_exceptionHandler.LastException is null)
            return;

        ExitApp(exitCode);
    }

    protected virtual void ExitApp() => ExitApp(1);

    protected virtual void ExitApp(int exitCode) => Environment.Exit(exitCode);

    protected virtual bool HasInitializationFailed() => _exceptionHandler.LastException is not null;

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
    /// Raises the <see cref="E:System.Windows.Application.Startup" /> event.
    /// </summary>
    /// <param name="e">A <see cref="StartupEventArgs" /> that contains the event data.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            FExCoreStatics.MainThreadContextProvider.SetMainThread();
            EnsureSingleInstance();

#pragma warning disable IDISP004 // intentional using(_=LogToHub) pattern for scoped status logging
            using (_ = LogToHub("Initializing app"))
            {
                OnConstruction(e);
                BeforeInitializationCheck();
                ComponentInitialize();
            }

            using (_ = LogToHub("Preparing app"))
                BeforeStartup(e);

            using (_ = LogToHub("Initializing app components"))
                AfterServicesContainerBuild();

            _ = LogToHub("Showing window");
            base.OnStartup(e);

            using (_ = LogToHub("Finalizing startup"))
                AfterStartup(e);
#pragma warning restore IDISP004
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {
            ExitIfInitializationHasFailed();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        FExMvvm.MessagePopupService.AppIsClosing = true;
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    protected DisposableAction LogToHub(string status) => _statusService.Log(status);
}