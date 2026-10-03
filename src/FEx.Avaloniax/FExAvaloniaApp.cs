using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using FEx.AppStartup;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Core.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using System;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

public abstract class FExAvaloniaApp<TContainer> : Application
    where TContainer : class, IFExContainer, IDisposable, new()
{
    private TContainer? _container;
    private Window? _startupWindow;
    private ShutdownMode _configuredShutdownMode;

    /// <summary>
    /// The service container. It is built asynchronously in <see cref="OnFrameworkInitializationCompleted" />, so it is
    /// available from <see cref="OnActivation" /> onwards; reading it earlier (for example in a constructor) throws.
    /// </summary>
    public TContainer Container => _container ?? throw new InvalidOperationException(
        "The service container is built asynchronously during OnFrameworkInitializationCompleted; it is available from OnActivation onwards.");

    protected FExAvaloniaApp() => AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;

    /// <summary>
    /// Optional window shown while the service container is built. It is closed as soon as the container is ready;
    /// the app does not shut down when it closes. Runs before the container exists, so it must not use container services.
    /// </summary>
    protected virtual Window? CreateStartupWindow() => null;

    /// <summary>Runs once the container is built. Create the main window in <see cref="AfterServicesContainerBuild" />.</summary>
    protected virtual void OnActivation()
    {
    }

    /// <summary>Runs after <see cref="OnActivation" />; the place to assign the lifetime's main window or main view.</summary>
    protected virtual void AfterServicesContainerBuild()
    {
    }

    /// <summary>
    /// Shows the optional startup window, awaits the container build without blocking the UI thread, then runs
    /// <see cref="OnActivation" /> and <see cref="AfterServicesContainerBuild" />. A failure goes through
    /// <see cref="HandleCriticalException" /> and exits with a non-zero code.
    /// </summary>
#pragma warning disable VSTHRD100 // async void is the only way to await inside this override; the sequencer catches everything.
    public sealed override async void OnFrameworkInitializationCompleted() =>
        await StartupSequencer.RunAsync(ShowStartupWindow,
                                        InitializeContainerAsync,
                                        CloseStartupWindow,
                                        [OnActivation, AfterServicesContainerBuild, CompleteInitialization],
                                        HandleCriticalException,
                                        ExitApp);
#pragma warning restore VSTHRD100

    private async Task InitializeContainerAsync() =>
        _container = await FExServiceProvider.InitializeAsync<TContainer>();

    // The startup window is the only window while the container builds; keep the app alive when it closes.
    private void ShowStartupWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        _configuredShutdownMode = desktop.ShutdownMode;
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _startupWindow = CreateStartupWindow();
        _startupWindow?.Show();
    }

    private void CloseStartupWindow()
    {
        if (_startupWindow is null)
            return;

        _startupWindow.Close();
        _startupWindow = null;
    }

    private void CompleteInitialization()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.ShutdownMode = _configuredShutdownMode;

        base.OnFrameworkInitializationCompleted();
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
