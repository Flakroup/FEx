using System;
using System.Threading.Tasks;

namespace FEx.Common.Startup;

/// <summary>
/// What <see cref="DesktopStartupHost{TWindow,TShutdownMode}" /> needs from a desktop app: the framework primitives
/// (windows, shutdown mode, startup document) and the app's startup hooks. The WPF and Avalonia bootstraps implement
/// it with one-line forwards, so the startup logic itself is testable against a fake.
/// </summary>
/// <typeparam name="TWindow">The framework's window type.</typeparam>
/// <typeparam name="TShutdownMode">The framework's shutdown mode type.</typeparam>
public interface IDesktopApp<TWindow, TShutdownMode>
    where TWindow : class
{
    /// <summary>The framework-loaded startup document (WPF <c>StartupUri</c>), or <c>null</c>.</summary>
    Uri? StartupUri { get; }

    /// <summary>The app's shutdown mode.</summary>
    TShutdownMode ShutdownMode { get; set; }

    /// <summary>The shutdown mode that keeps the app alive when windows close.</summary>
    TShutdownMode ExplicitShutdownMode { get; }

    /// <summary>The window the framework treats as the app's main window.</summary>
    TWindow? MainWindow { get; set; }

    /// <summary>Creates the optional startup window; runs before the container exists.</summary>
    TWindow? CreateStartupWindow();

    /// <summary>Creates the main window; runs after the container is built and all hooks ran.</summary>
    TWindow? CreateMainWindow();

    /// <summary>Shows a window.</summary>
    void Show(TWindow window);

    /// <summary>Closes a window.</summary>
    void Close(TWindow window);

    /// <summary>Runs once the main window (if any) is shown, e.g. to raise the framework's startup event.</summary>
    void OnMainWindowShown();

    /// <summary>Builds the service container.</summary>
    Task InitializeContainerAsync();

    /// <summary>Marks the current thread as the main thread.</summary>
    void SetMainThread();

    /// <summary>Resolves the services the app exposes to its subclasses.</summary>
    void PublishServices();

    /// <summary>First hook once the services are published.</summary>
    void OnActivation();

    /// <summary>Single-instance check.</summary>
    void EnsureSingleInstance();

    /// <summary>Component initialization hooks.</summary>
    void InitializeComponents();

    /// <summary>Hook before the app starts.</summary>
    void BeforeStartup();

    /// <summary>Hook after the container is built, before the main window is created.</summary>
    void AfterServicesContainerBuild();

    /// <summary>Hook after the main window is shown.</summary>
    void AfterStartup();

    /// <summary>Exits if initialization recorded a failure.</summary>
    void ExitIfInitializationHasFailed();

    /// <summary>Reports a failure before the services were published; must work without the container.</summary>
    void HandleEarlyException(Exception exception);

    /// <summary>The app's overridable exception handling.</summary>
    void HandleException(Exception exception);

    /// <summary>Requests process exit with the given code.</summary>
    void RequestExit(int exitCode);
}
