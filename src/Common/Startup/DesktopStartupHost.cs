using System;
using System.Threading.Tasks;

namespace FEx.Common.Startup;

/// <summary>
/// The startup flow for a desktop app, driven through an <see cref="IDesktopApp{TWindow,TShutdownMode}" />: the startup
/// window while the container builds, the shutdown mode while it is the only window, the <c>StartupUri</c> rejection,
/// and the main window created and shown after init.
/// </summary>
/// <typeparam name="TWindow">The framework's window type.</typeparam>
/// <typeparam name="TShutdownMode">The framework's shutdown mode type.</typeparam>
/// <param name="app">The app surface the flow drives.</param>
public sealed class DesktopStartupHost<TWindow, TShutdownMode>(IDesktopApp<TWindow, TShutdownMode> app) : StartupHost
    where TWindow : class
{
    private TWindow? _startupWindow;
    private TShutdownMode _configuredShutdownMode = default!;

    /// <inheritdoc />
    protected override void PrepareStartup()
    {
        if (app.StartupUri is not null)
            throw new InvalidOperationException(
                "StartupUri is not supported: the framework would load it before the service container is built. "
                + "Remove it from App.xaml and return the main window from CreateMainWindow().");
    }

    /// <inheritdoc />
    protected override void SetMainThread() => app.SetMainThread();

    /// <inheritdoc />
    protected override void SuspendShutdown()
    {
        _configuredShutdownMode = app.ShutdownMode;
        app.ShutdownMode = app.ExplicitShutdownMode;
    }

    /// <inheritdoc />
    protected override void ShowStartupWindow()
    {
        _startupWindow = app.CreateStartupWindow();

        if (_startupWindow is not null)
            app.Show(_startupWindow);
    }

    /// <inheritdoc />
    protected override Task InitializeContainerAsync() => app.InitializeContainerAsync();

    /// <inheritdoc />
    protected override void CloseStartupWindow()
    {
        if (_startupWindow is null)
            return;

        app.Close(_startupWindow);

        // The first window created becomes the main window; the startup window must not keep that slot.
        if (ReferenceEquals(app.MainWindow, _startupWindow))
            app.MainWindow = null;

        _startupWindow = null;
    }

    /// <inheritdoc />
    protected override void RestoreShutdown() => app.ShutdownMode = _configuredShutdownMode;

    /// <inheritdoc />
    protected override void ShowMainWindow()
    {
        if (app.CreateMainWindow() is { } mainWindow)
        {
            app.MainWindow = mainWindow;
            app.Show(mainWindow);
        }

        app.OnMainWindowShown();
    }

    /// <inheritdoc />
    protected override void PublishServices() => app.PublishServices();

    /// <inheritdoc />
    protected override void OnActivation() => app.OnActivation();

    /// <inheritdoc />
    protected override void EnsureSingleInstance() => app.EnsureSingleInstance();

    /// <inheritdoc />
    protected override void InitializeComponents() => app.InitializeComponents();

    /// <inheritdoc />
    protected override void BeforeStartup() => app.BeforeStartup();

    /// <inheritdoc />
    protected override void AfterServicesContainerBuild() => app.AfterServicesContainerBuild();

    /// <inheritdoc />
    protected override void AfterStartup() => app.AfterStartup();

    /// <inheritdoc />
    protected override void ExitIfInitializationHasFailed() => app.ExitIfInitializationHasFailed();

    /// <inheritdoc />
    protected override void HandleEarlyException(Exception exception) => app.HandleEarlyException(exception);

    /// <inheritdoc />
    protected override void HandleException(Exception exception) => app.HandleException(exception);

    /// <inheritdoc />
    protected override void RequestExit(int exitCode) => app.RequestExit(exitCode);
}
