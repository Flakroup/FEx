using System;
using System.Threading.Tasks;

namespace FEx.Common.Startup;

/// <summary>
/// UI-framework agnostic startup flow shared by the WPF and Avalonia bootstrap base classes. The bootstraps provide
/// the framework primitives by overriding the steps; the order lives here, so it is testable without an <c>Application</c>.
/// <para>
/// Order: <see cref="PrepareStartup" />, <see cref="SuspendShutdown" />, <see cref="ShowStartupWindow" />,
/// await <see cref="InitializeContainerAsync" />, <see cref="CloseStartupWindow" />, <see cref="RestoreShutdown" />
/// (these two also run when the container build fails), then <see cref="PublishServices" />, <see cref="OnActivation" />,
/// <see cref="EnsureSingleInstance" />, <see cref="InitializeComponents" />, <see cref="BeforeStartup" />,
/// <see cref="AfterServicesContainerBuild" />, <see cref="ShowMainWindow" />, <see cref="AfterStartup" />,
/// <see cref="ExitIfInitializationHasFailed" />. Any exception goes to <see cref="HandleException" /> and
/// <see cref="RequestExit" /> is called with <see cref="FailureExitCode" />.
/// </para>
/// </summary>
internal abstract class StartupHost
{
    internal const int FailureExitCode = 1;

    internal async Task RunAsync()
    {
        var failed = false;

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

            PublishServices();
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

    /// <summary>Keeps the app alive while only the startup window exists.</summary>
    protected abstract void SuspendShutdown();

    protected abstract void ShowStartupWindow();
    protected abstract Task InitializeContainerAsync();

    /// <summary>Closes the startup window and releases the main-window slot it held.</summary>
    protected abstract void CloseStartupWindow();

    /// <summary>Gives back the shutdown mode the app had before <see cref="SuspendShutdown" />.</summary>
    protected abstract void RestoreShutdown();

    /// <summary>Creates and shows the main window after init (frameworks do not show a window assigned later).</summary>
    protected abstract void ShowMainWindow();

    protected abstract void HandleException(Exception exception);
    protected abstract void RequestExit(int exitCode);

    protected virtual void PublishServices()
    {
    }

    protected virtual void OnActivation()
    {
    }

    protected virtual void EnsureSingleInstance()
    {
    }

    protected virtual void InitializeComponents()
    {
    }

    protected virtual void BeforeStartup()
    {
    }

    protected virtual void AfterServicesContainerBuild()
    {
    }

    protected virtual void AfterStartup()
    {
    }

    protected virtual void ExitIfInitializationHasFailed()
    {
    }
}
