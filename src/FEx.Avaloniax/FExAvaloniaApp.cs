using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using FEx.Common.Startup;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Core.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using System;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

public abstract class FExAvaloniaApp<TContainer> : Application
    where TContainer : class, IFExContainer, IDisposable, new()
{
    private readonly Host _host;
    private TContainer? _container;
    private Window? _startupWindow;
    private ShutdownMode _configuredShutdownMode;

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

    /// <summary>Adapts the Avalonia <see cref="Application" /> to the shared <see cref="StartupHost" /> flow.</summary>
    private sealed class Host(FExAvaloniaApp<TContainer> app) : StartupHost
    {
        private IClassicDesktopStyleApplicationLifetime? Desktop => app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        protected override void SuspendShutdown()
        {
            if (Desktop is not { } desktop)
                return;

            app._configuredShutdownMode = desktop.ShutdownMode;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        protected override void ShowStartupWindow()
        {
            if (Desktop is null)
                return;

            app._startupWindow = app.CreateStartupWindow();
            app._startupWindow?.Show();
        }

        protected override async Task InitializeContainerAsync() =>
            app._container = await FExServiceProvider.InitializeAsync<TContainer>();

        protected override void CloseStartupWindow()
        {
            app._startupWindow?.Close();
            app._startupWindow = null;
        }

        protected override void RestoreShutdown()
        {
            if (Desktop is { } desktop)
                desktop.ShutdownMode = app._configuredShutdownMode;
        }

        protected override void OnActivation() => app.OnActivation();

        protected override void AfterServicesContainerBuild() => app.AfterServicesContainerBuild();

        protected override void ShowMainWindow()
        {
            if (Desktop is { } desktop && app.CreateMainWindow() is { } mainWindow)
            {
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
            }

            app.RaiseInitializationCompleted();
        }

        protected override void HandleException(Exception exception) => app.HandleCriticalException(exception);

        protected override void RequestExit(int exitCode) => app.ExitApp(exitCode);
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
