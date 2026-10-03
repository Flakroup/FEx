using System;
using System.Threading.Tasks;

namespace FEx.Common.Startup;

/// <summary>
/// UI-framework agnostic startup flow shared by the WPF and Avalonia bootstrap base classes. The order lives here; the
/// bootstraps provide the framework primitives by overriding the steps.
/// <para>
/// Order: <see cref="PrepareStartup" />, <see cref="SetMainThread" /> (on whatever provider exists before the container,
/// so a startup window can use the static dispatcher), <see cref="SuspendShutdown" />, <see cref="ShowStartupWindow" />,
/// await <see cref="InitializeContainerAsync" />, <see cref="CloseStartupWindow" />, <see cref="RestoreShutdown" />
/// (these two also run when the container build fails), <see cref="SetMainThread" /> again (now on the container's own
/// provider), <see cref="PublishServices" />, <see cref="OnActivation" />, <see cref="EnsureSingleInstance" />,
/// <see cref="InitializeComponents" />, <see cref="BeforeStartup" />, <see cref="AfterServicesContainerBuild" />,
/// <see cref="ShowMainWindow" />, <see cref="AfterStartup" />, <see cref="ExitIfInitializationHasFailed" />.
/// </para>
/// <para>
/// An exception first goes to <see cref="HandleEarlyException" /> if it happened before the services were published,
/// then always to <see cref="HandleException" />, and finally <see cref="RequestExit" /> is called with
/// <see cref="FailureExitCode" />.
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
            SetMainThread();

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

            // Again, now that the container exists, so it reaches the container's own provider and not the static fallback.
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

            if (!servicesReady)
                HandleEarlyException(ex);

            HandleException(ex);
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

    /// <summary>Marks the current thread as the main thread; runs before the container (static fallback) and right after it.</summary>
    protected virtual void SetMainThread()
    {
    }

    /// <summary>Keeps the app alive while only the startup window exists.</summary>
    protected abstract void SuspendShutdown();

    /// <summary>Shows the optional startup window; runs before the container exists.</summary>
    protected abstract void ShowStartupWindow();

    /// <summary>Builds the service container; awaited, never blocked on.</summary>
    protected abstract Task InitializeContainerAsync();

    /// <summary>Closes the startup window and releases the main-window slot it held.</summary>
    protected abstract void CloseStartupWindow();

    /// <summary>Gives back the shutdown mode the app had before <see cref="SuspendShutdown" />.</summary>
    protected abstract void RestoreShutdown();

    /// <summary>Creates and shows the main window after init (frameworks do not show a window assigned later).</summary>
    protected abstract void ShowMainWindow();

    /// <summary>
    /// Reports a failure that happened before the services were published (a rejected configuration, a failed
    /// container build). Must work without the container; runs before <see cref="HandleException" />.
    /// </summary>
    protected virtual void HandleEarlyException(Exception exception)
    {
    }

    /// <summary>Handles every startup failure; the app's overridable exception handling.</summary>
    protected abstract void HandleException(Exception exception);

    /// <summary>Requests process exit; called only on failure.</summary>
    protected abstract void RequestExit(int exitCode);

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
