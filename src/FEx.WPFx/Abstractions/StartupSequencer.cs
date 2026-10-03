using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.AppStartup;

/// <summary>
/// UI-framework agnostic startup ordering shared by the WPF and Avalonia bootstrap base classes (this file is
/// compiled into both assemblies). Keeps the order testable without a real <c>Application</c>:
/// show the startup window, await the container, close the startup window, run the hooks in order. A failure at any
/// point is reported to <c>handleException</c> and requests a non-zero exit.
/// </summary>
internal static class StartupSequencer
{
    internal const int FailureExitCode = 1;

    /// <param name="showStartupWindow">Shows the optional startup window and prepares the shutdown mode.</param>
    /// <param name="initializeContainer">Builds the DI container; awaited, never blocked on.</param>
    /// <param name="closeStartupWindow">Closes the startup window; runs even if the container build failed.</param>
    /// <param name="hooks">Hooks that run in order once the container exists; the first one publishes the services.</param>
    /// <param name="handleException">Receives the first exception of the sequence.</param>
    /// <param name="requestExit">Requests process exit with the given code; called only on failure.</param>
    internal static async Task RunAsync(Action showStartupWindow,
                                        Func<Task> initializeContainer,
                                        Action closeStartupWindow,
                                        IReadOnlyList<Action> hooks,
                                        Action<Exception> handleException,
                                        Action<int> requestExit)
    {
        var failed = false;

        try
        {
            showStartupWindow();

            try
            {
                await initializeContainer();
            }
            finally
            {
                closeStartupWindow();
            }

            foreach (var hook in hooks)
                hook();
        }
        catch (Exception ex)
        {
            failed = true;
            handleException(ex);
        }
        finally
        {
            if (failed)
                requestExit(FailureExitCode);
        }
    }
}
