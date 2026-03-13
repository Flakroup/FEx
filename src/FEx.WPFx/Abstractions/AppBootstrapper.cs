using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Common.Abstractions.Interfaces;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using FEx.Core.Abstractions.Utilities;
using FEx.DependencyInjection.Abstractions;
using FEx.MVVM;
using FEx.MVVM.Abstractions.Enums;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.WpfBindingErrors;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Threading;

namespace FEx.WPFx.Abstractions;

public abstract class AppBootstrapper<TContainer> : Application
    where TContainer : class, IFExContainer, IDisposable, new()
{
    protected readonly IAppInfoProvider _appInfoProvider;
    protected readonly IExceptionHandler _exceptionHandler;
    protected readonly IStatusService _statusService;
    protected readonly IAppConfig _appConfig;
    protected readonly TContainer _container;

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

            _container = FExServiceProvider.InitializeAsync<TContainer>().GetAwaiter().GetResult();
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
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                               | SecurityProtocolType.Tls11
                                               | SecurityProtocolType.Tls12
                                               | SecurityProtocolType.Tls13;

        ServicePointManager.UseNagleAlgorithm = false;
    }

    protected virtual void HandleException(Exception exception) => exception.HandleException(true, true);

    protected virtual void BeforeStartup(StartupEventArgs e)
    {
        FExWpfx.OverrideFormattingOnUI();

        _appConfig.Initialize();

        ExitIfInitializationHasFailed();
    }

    protected virtual void ExitIfInitializationHasFailed(int exitCode = 1)
    {
        if (_exceptionHandler.LastException is null)
            return;

        ExitApp(exitCode);
    }

    protected virtual void ExitApp(int exitCode = 1) => Environment.Exit(exitCode);

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
        using (_ = LogToHub("Checking duplicated instances"))
        {
            var otherInstances = AppUtility.GetOtherInstances();
            var isSingleInstance = otherInstances.Length == 0;

            if (isSingleInstance)
                return;

            if (FExMvvm.MessagePopupService.ShowMessage(
                    $"{_appInfoProvider?.Name ?? "App"} is already running.{Environment.NewLine}Do you want to close it?",
                    "Duplicated instance",
                    button: FExMessageButton.YesNo)
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
            EnsureSingleInstance();

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