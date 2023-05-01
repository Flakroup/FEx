using FEx.Abstractions;
using FEx.Fundamentals;
using System;
using System.Windows;
using System.Windows.Threading;
using WpfBindingErrors;

namespace FEx.WPFx;

public abstract class AppBootstrapper : Application
{
    private readonly IExceptionHandler _exceptionHandler;

    protected IFExServiceProvider IoCProvider => Foundation.ServiceProvider;

    protected AppBootstrapper()
    {
        ConfigureServiceProvider();

        _exceptionHandler = IoCProvider.GetRequiredService<IExceptionHandler>();
        AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
        DispatcherUnhandledException += OnAppDispatcherUnhandledException;
    }

    protected abstract void ComponentInitialize();

    protected virtual void HandleAppException(Exception exception)
    {
        if (_exceptionHandler.CanHandle(exception))
            _exceptionHandler.Handle(exception);
    }

    protected virtual void BeforeStartup(StartupEventArgs e)
    {
        WPFUtilities.OverrideFormattingOnUI();

        //BeforeInitializationCheck();

        ExitIfInitializationHasFailed();
    }

    protected virtual void ExitIfInitializationHasFailed()
    {
        if (HasInitializationFailed())
            Environment.Exit(1);
    }

    protected virtual bool HasInitializationFailed() =>
        //return !HasBeenInitialized || ExceptionHandler.LastException != null;
        false;

    protected virtual void AfterStartup(StartupEventArgs e)
    {
        BindingExceptionThrower.Attach();
    }

    /// <summary>
    ///     Raises the <see cref="E:System.Windows.Application.Startup" /> event.
    /// </summary>
    /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs" /> that contains the event data.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            EnsureSingleInstance();
            //Guid s = LogToHub("Initializing app");
            //OnConstruction(e);
            Initialize();
            //RemoveLog(s);
            //s = LogToHub("Preparing app");
            //BeforeStartup(e);
            //RemoveLog(s);
            //s = LogToHub("Initializing app components");
            //Helper.ConfigureServiceProvider(services =>
            //    ConfigureServices(services
            //        .AddSingleton(DispatcherContextExecutor.Instance)
            //        .AddSingleton<IUIContextExecutor>(DispatcherContextExecutor.Instance)
            //        .AddSingleton(DefaultUIContextExecutor.Instance)));
            //AfterServicesContainerBuild();
            //RemoveLog(s);
            //LogToHub("Window creation");
            //base.OnStartup(e);
            //WpfCommon.SetGlobalUIContextExecutor();
            //s = LogToHub("Finalizing startup");
            //AfterStartup(e);
            //RemoveLog(s);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            //HandleCriticalException(ex);
            //HasBeenInitialized = false;
        }
    }

    private void ConfigureServiceProvider()
    {
    }

    private void OnAppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Exception != null)
            HandleAppException(e.Exception);

        e.Handled = true;
    }

    private void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            HandleAppException(exception);
    }

    private void Initialize()
    {
        ComponentInitialize();
        //HasBeenInitialized = true;
    }

    private void EnsureSingleInstance()
    {
        //Guid s = LogToHub("Checking duplicated instances");
        //if (!CommonServicesModule.EnsureSingleInstance())
        //{
        //    if (WpfCommon.ShowMessageBox(
        //        $"{CommonServicesModule.ApplicationName} is already running.{Environment.NewLine}Do you want to close it?",
        //        buttons: MessageBoxButton.YesNo) == MessageBoxResult.No)
        //    {
        //        Environment.Exit(0);
        //    }
        //    else
        //    {
        //        foreach (int pid in CommonServicesModule.GetOtherInstances())
        //        {
        //            using (var p = Process.GetProcessById(pid))
        //            {
        //                p.Kill();
        //            }
        //        }
        //    }
        //}
        //RemoveLog(s);
    }
}