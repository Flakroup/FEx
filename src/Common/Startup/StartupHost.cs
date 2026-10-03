using System;
using System.Threading.Tasks;

namespace FEx.Common.Startup;

/// <summary>
/// UI-framework agnostic startup flow shared by the WPF and Avalonia bootstrap base classes. The bootstraps provide
/// the framework primitives by overriding the steps; the order lives here, so it is testable without an <c>Application</c>.
/// <para>
/// Order: <see cref="PrepareStartup" />, <see cref="SuspendShutdown" />, <see cref="ShowStartupWindow" />,
/// await <see cref="InitializeContainerAsync" />, <see cref="CloseStartupWindow" />, <see cref="RestoreShutdown" />
/// (these two also run when the container build fails), then <see cref="SetMainThread" />, <see cref="PublishServices" />, <see cref="OnActivation" />,
/// <see cref="EnsureSingleInstance" />, <see cref="InitializeComponents" />, <see cref="BeforeStartup" />,
/// <see cref="AfterServicesContainerBuild" />, <see cref="ShowMainWindow" />, <see cref="AfterStartup" />,
/// <see cref="ExitIfInitializationHasFailed" />. An exception goes to <see cref="HandleEarlyException" /> before the
/// services are published and to <see cref="HandleException" /> afterwards; then
/// <see cref="RequestExit" /> is called with <see cref="FailureExitCode" />.
/// </para>
/// </summary>
public abstract class StartupHost
{
    /// <summary>The process exit code requested when startup fails.</summary>
    public const int FailureExitCode = 1;

    /// <summary>Runs the startup flow; never throws, failures go through the handlers and <see cref="RequestExit" />.</summary>
    public async Task RunAsync()
    {
        var failed = false;
        var servicesReady = false;

        try
        {
            PrepareStartup();

            SuspendShutdown();
            try
            {
                ShowStartupWindow();
                await InitializeContainerAsync();
            }
            finally
            {
                // Close first (shutdown is still suspended), then give the shutdown mode back, before any hook runs, so a
                // mode set by a hook (e.g. App.xaml applied in InitializeComponent) is never overwritten.
                CloseStartupWindow();
                RestoreShutdown();
            }

            // First thing once the container exists, so it reaches the container's own provider and not the static fallback.
            SetMainThread();
            PublishServices();
            servicesReady = true;
            OnActivation();
            EnsureSingleInstance();
            InitializeComponents();
            BeforeStartup();
            AfterServicesContainerBuild();
            ShowMainWindow();
            AfterStartup();
            ExitIfInitializationHasFailed();
        }
        catch (Exception ex)
        {
            failed = true;

            // Before the services are published the container-based exception handling is not configured yet.
            if (servicesReady)
                HandleException(ex);
            else
                HandleEarlyException(ex);
        }
        finally
        {
            if (failed)
                RequestExit(FailureExitCode);
        }
    }

    /// <summary>Synchronous validation before the first await; throw to reject the app configuration.</summary>
    protected virtual void PrepareStartup()
    {
    }

    /// <summary>Keeps the app alive while only the startup window exists.</summary>
    /// <summary>Keeps the app alive while only the startup window exists.</summary>
    protected abstract void SuspendShutdown();

    /// <summary>Shows the optional startup window; runs before the container exists.</summary>
    protected abstract void ShowStartupWindow();
    /// <summary>Builds the service container; awaited, never blocked on.</summary>
    protected abstract Task InitializeContainerAsync();

    /// <summary>Closes the startup window and releases the main-window slot it held.</summary>
    /// <summary>Closes the startup window and releases the main-window slot it held.</summary>
    protected abstract void CloseStartupWindow();

    /// <summary>Gives back the shutdown mode the app had before <see cref="SuspendShutdown" />.</summary>
    /// <summary>Gives back the shutdown mode the app had before <see cref="SuspendShutdown" />.</summary>
    protected abstract void RestoreShutdown();

    /// <summary>Creates and shows the main window after init (frameworks do not show a window assigned later).</summary>
    /// <summary>Creates and shows the main window after init (frameworks do not show a window assigned later).</summary>
    protected abstract void ShowMainWindow();

    /// <summary>Handles a failure once the services are published.</summary>
    protected abstract void HandleException(Exception exception);
    /// <summary>Requests process exit; called only on failure.</summary>
    protected abstract void RequestExit(int exitCode);

    /// <summary>
    /// Handles a failure before the services are published (for example a rejected configuration or a failed container
    /// build). Must work without the container; the default forwards to <see cref="HandleException" />.
    /// </summary>
    protected virtual void HandleEarlyException(Exception exception) => HandleException(exception);

    /// <summary>Marks the current thread as the main thread; runs right after the container is built.</summary>
    protected virtual void SetMainThread()
    {
    }

    /// <summary>Resolves the services the app exposes to its subclasses.</summary>
    protected virtual void PublishServices()
    {
    }

    /// <summary>First hook once the services are published.</summary>
    protected virtual void OnActivation()
    {
    }

    /// <summary>Single-instance check.</summary>
    protected virtual void EnsureSingleInstance()
    {
    }

    /// <summary>Component initialization hooks.</summary>
    protected virtual void InitializeComponents()
    {
    }

    /// <summary>Hook before the app starts.</summary>
    protected virtual void BeforeStartup()
    {
    }

    /// <summary>Hook after the container is built, before the main window is created.</summary>
    protected virtual void AfterServicesContainerBuild()
    {
    }

    /// <summary>Hook after the main window is shown.</summary>
    protected virtual void AfterStartup()
    {
    }

    /// <summary>Exits if initialization recorded a failure.</summary>
    protected virtual void ExitIfInitializationHasFailed()
    {
    }
}
