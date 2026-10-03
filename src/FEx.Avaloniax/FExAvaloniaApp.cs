using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using FEx.Common.Startup;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Core.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

public abstract class FExAvaloniaApp<TContainer> : Application
    where TContainer : class, IFExContainer, IDisposable, new()
{
    private readonly Host _host;
    private TContainer? _container;

    /// <summary>
    /// The service container. It is built asynchronously in <see cref="OnFrameworkInitializationCompleted" />, so it is
    /// available from <see cref="OnActivation" /> onwards; reading it earlier (for example in a constructor) throws.
    /// </summary>
    public TContainer Container => _container ?? throw new InvalidOperationException(
        "The service container is built asynchronously during OnFrameworkInitializationCompleted; it is available from OnActivation onwards.");

    protected FExAvaloniaApp()
    {
        _host = new Host(this);
        AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
    }

    /// <summary>
    /// Optional window shown while the service container is built. It is closed as soon as the container is ready;
    /// the app does not shut down when it closes. Runs before the container exists, so it must not use container services.
    /// </summary>
    protected virtual Window? CreateStartupWindow() => null;

    /// <summary>
    /// Creates the main window once the container is built; the base assigns it to the desktop lifetime and shows it
    /// (Avalonia shows the lifetime's main window only once, before this async flow resumes, so a window assigned
    /// later would never appear). Return <c>null</c> for non-desktop lifetimes or if the app shows its windows itself.
    /// </summary>
    protected virtual Window? CreateMainWindow() => null;

    /// <summary>Runs once the container is built.</summary>
    protected virtual void OnActivation()
    {
    }

    /// <summary>Runs after <see cref="OnActivation" />, before the main window is created.</summary>
    protected virtual void AfterServicesContainerBuild()
    {
    }

    /// <summary>
    /// Shows the optional startup window, awaits the container build without blocking the UI thread, then runs
    /// <see cref="OnActivation" />, <see cref="AfterServicesContainerBuild" /> and shows the window from
    /// <see cref="CreateMainWindow" />. A failure goes through <see cref="HandleCriticalException" /> and exits with a
    /// non-zero code.
    /// </summary>
#pragma warning disable VSTHRD100 // async void is the only way to await inside this override; the host catches everything.
    public sealed override async void OnFrameworkInitializationCompleted() => await _host.RunAsync();
#pragma warning restore VSTHRD100

    private void RaiseInitializationCompleted() => base.OnFrameworkInitializationCompleted();

    /// <summary>Forwards the shared <see cref="DesktopStartupHost{TWindow,TShutdownMode}" /> primitives to the Avalonia desktop lifetime.</summary>
    private sealed class Host(FExAvaloniaApp<TContainer> app) : DesktopStartupHost<Window, ShutdownMode>
    {
        private IClassicDesktopStyleApplicationLifetime? Desktop => app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        // Non-desktop lifetimes (single view) have no shutdown mode and no windows: these primitives become no-ops.
        protected override ShutdownMode ShutdownMode
        {
            get => Desktop?.ShutdownMode ?? default;
            set
            {
                if (Desktop is { } desktop)
                    desktop.ShutdownMode = value;
            }
        }

        protected override ShutdownMode ExplicitShutdownMode => ShutdownMode.OnExplicitShutdown;

        protected override Window? MainWindow
        {
            get => Desktop?.MainWindow;
            set
            {
                if (Desktop is { } desktop)
                    desktop.MainWindow = value;
            }
        }

        protected override Window? CreateStartupWindow() => Desktop is null ? null : app.CreateStartupWindow();

        protected override Window? CreateMainWindow() => Desktop is null ? null : app.CreateMainWindow();

        protected override void Show(Window window) => window.Show();

        protected override void Close(Window window) => window.Close();

        protected override void OnMainWindowShown() => app.RaiseInitializationCompleted();

        protected override async Task InitializeContainerAsync() =>
            app._container = await FExServiceProvider.InitializeAsync<TContainer>();

        protected override void OnActivation() => app.OnActivation();

        protected override void AfterServicesContainerBuild() => app.AfterServicesContainerBuild();

        protected override void HandleException(Exception exception) => app.HandleCriticalException(exception);

        protected override void HandleEarlyException(Exception exception) => app.HandleEarlyException(exception);

        protected override void RequestExit(int exitCode) => app.ExitApp(exitCode);
    }

    /// <summary>
    /// Handles a startup failure before the service container exists, when <see cref="HandleCriticalException" /> cannot
    /// rely on the container's exception handling. Writes to the trace output and standard error.
    /// </summary>
    protected virtual void HandleEarlyException(Exception exception)
    {
        Trace.TraceError(exception.ToString());
        Console.Error.WriteLine(exception);
    }

    protected virtual void ExitApp(int exitCode) => Environment.Exit(exitCode);

    protected virtual void HandleCriticalException(Exception ex) => ex.HandleException(true, true);

    protected virtual void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            HandleAppException(exception);
    }

    protected virtual void HandleAppException(Exception exception)
    {
        var bindingException = exception is BindingChainException;
        exception.HandleException(!bindingException);
    }
}
