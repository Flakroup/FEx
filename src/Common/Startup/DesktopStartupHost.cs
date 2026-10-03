using System;

namespace FEx.Common.Startup;

/// <summary>
/// The window handling every desktop UI framework needs during startup, written once and driven through a few
/// framework primitives: the startup window while the container builds, the shutdown mode while it is the only window,
/// and the main window created and shown after init.
/// </summary>
/// <typeparam name="TWindow">The framework's window type.</typeparam>
/// <typeparam name="TShutdownMode">The framework's shutdown mode type.</typeparam>
public abstract class DesktopStartupHost<TWindow, TShutdownMode> : StartupHost
    where TWindow : class
{
    private TWindow? _startupWindow;
    private TShutdownMode _configuredShutdownMode = default!;

    /// <summary>The app's shutdown mode.</summary>
    protected abstract TShutdownMode ShutdownMode { get; set; }

    /// <summary>The shutdown mode that keeps the app alive when windows close.</summary>
    protected abstract TShutdownMode ExplicitShutdownMode { get; }

    /// <summary>The window the framework treats as the app's main window.</summary>
    protected abstract TWindow? MainWindow { get; set; }

    /// <summary>Creates the optional startup window; runs before the container exists.</summary>
    protected abstract TWindow? CreateStartupWindow();

    /// <summary>Creates the main window; runs after the container is built and all hooks ran.</summary>
    protected abstract TWindow? CreateMainWindow();

    /// <summary>Shows a window.</summary>
    protected abstract void Show(TWindow window);

    /// <summary>Closes a window.</summary>
    protected abstract void Close(TWindow window);

    /// <summary>
    /// Whether the app declares a framework-loaded startup document (WPF <c>StartupUri</c>); such a document would be
    /// loaded as soon as the async startup first yields, before the container exists, so it is rejected.
    /// </summary>
    protected virtual bool HasStartupUri => false;

    /// <summary>Runs once the main window (if any) is shown, e.g. to raise the framework's startup event.</summary>
    protected virtual void OnMainWindowShown()
    {
    }

    /// <inheritdoc />
    protected override void PrepareStartup()
    {
        if (HasStartupUri)
            throw new InvalidOperationException(
                "StartupUri is not supported: the framework would load it before the service container is built. "
                + "Remove it from App.xaml and return the main window from CreateMainWindow().");
    }

    /// <inheritdoc />
    protected override void SuspendShutdown()
    {
        _configuredShutdownMode = ShutdownMode;
        ShutdownMode = ExplicitShutdownMode;
    }

    /// <inheritdoc />
    protected override void ShowStartupWindow()
    {
        _startupWindow = CreateStartupWindow();

        if (_startupWindow is not null)
            Show(_startupWindow);
    }

    /// <inheritdoc />
    protected override void CloseStartupWindow()
    {
        if (_startupWindow is null)
            return;

        Close(_startupWindow);

        // The first window created becomes the main window; the startup window must not keep that slot.
        if (ReferenceEquals(MainWindow, _startupWindow))
            MainWindow = null;

        _startupWindow = null;
    }

    /// <inheritdoc />
    protected override void RestoreShutdown() => ShutdownMode = _configuredShutdownMode;

    /// <inheritdoc />
    protected override void ShowMainWindow()
    {
        if (CreateMainWindow() is { } mainWindow)
        {
            MainWindow = mainWindow;
            Show(mainWindow);
        }

        OnMainWindowShown();
    }
}
