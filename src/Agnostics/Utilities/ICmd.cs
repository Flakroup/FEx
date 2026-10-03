using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace FEx.Agnostics.Utilities;

/// <summary>Represents a command-line process runner that captures output, exit code and timing.</summary>
public interface ICmd : IDisposable
{
    /// <summary>Gets the exit code of the last completed run, or <see langword="null"/> if none is available.</summary>
    int? Code { get; }
    /// <summary>Gets the standard error lines collected during the last run.</summary>
    StringBuilder ErrOut { get; }
    /// <summary>Gets the standard output lines collected during the last run.</summary>
    StringBuilder Output { get; }
    /// <summary>Gets the most recently started process.</summary>
    Process Proc { get; }
    /// <summary>Gets the start information used to launch the process.</summary>
    ProcessStartInfo StartInfo { get; }
    /// <summary>Gets the stopwatch that measures the duration of the last run.</summary>
    Stopwatch Stopwatch { get; }

    /// <summary>Starts the process asynchronously, collecting its output, and optionally waits for it to exit.</summary>
    /// <param name="args">The command-line arguments to pass to the process.</param>
    /// <param name="verb">An optional shell verb to apply to the start information.</param>
    /// <param name="waitForExit">Whether to wait for the process to exit and record its exit code.</param>
    /// <param name="onStarted">An optional callback awaited after the process has started.</param>
    /// <returns>A task that completes when the run has finished.</returns>
    Task RunAsync(string args, string? verb = null, bool waitForExit = true, Func<ICmd, Task>? onStarted = null);
}