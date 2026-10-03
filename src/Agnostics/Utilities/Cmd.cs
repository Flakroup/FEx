using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace FEx.Agnostics.Utilities;

/// <summary>Runs a command-line process with redirected output and captures its standard output, error output, exit code and elapsed time.</summary>
public class Cmd : ICmd
{
    private bool _isDisposed;

    // Invariant: StartInfo is set in the public constructor; Proc is set in StartProc before any use.
    /// <summary>Gets the start information used to launch the process.</summary>
    public ProcessStartInfo StartInfo { get; protected set; } = null!;
    /// <summary>Gets the most recently started process.</summary>
    public Process Proc { get; protected set; } = null!;
    /// <summary>Gets the exit code of the last completed run, or <see langword="null"/> if none has completed while waiting for exit.</summary>
    public int? Code { get; protected set; }
    /// <summary>Gets the standard output lines collected during the last run.</summary>
    public StringBuilder Output { get; }
    /// <summary>Gets the standard error lines collected during the last run.</summary>
    public StringBuilder ErrOut { get; }
    /// <summary>Gets the stopwatch that measures how long the last run took to start and, if awaited, finish.</summary>
    public Stopwatch Stopwatch { get; }

    /// <summary>Initializes a command runner that redirects output and error streams without showing a window.</summary>
    /// <param name="procName">The executable to start.</param>
    /// <param name="windowStyle">The window style for the process.</param>
    /// <param name="cfg">An optional callback to further customize the <see cref="ProcessStartInfo"/>.</param>
    public Cmd(string procName = "cmd",
               ProcessWindowStyle windowStyle = ProcessWindowStyle.Hidden,
               Action<ProcessStartInfo>? cfg = null)
        : this()
    {
        StartInfo = new()
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WindowStyle = windowStyle,
            CreateNoWindow = true,
            FileName = procName
        };

        cfg?.Invoke(StartInfo);
    }

    /// <summary>Initializes the output buffers and stopwatch for derived classes that set up <see cref="StartInfo"/> themselves.</summary>
    protected Cmd()
    {
        Code = null;
        Output = new(string.Empty);
        ErrOut = new(string.Empty);
        Stopwatch = new();
    }

    /// <summary>Starts the process asynchronously, collecting its output, and optionally waits for it to exit.</summary>
    /// <param name="args">The command-line arguments; prefixed with <c>/C</c> when the executable is <c>cmd</c>.</param>
    /// <param name="verb">An optional shell verb to apply to the start information.</param>
    /// <param name="waitForExit">Whether to wait for the process to exit and record its exit code.</param>
    /// <param name="onStarted">An optional callback awaited after the process has started.</param>
    /// <returns>A task that completes when the run has finished.</returns>
    public async Task RunAsync(string args,
                               string? verb = null,
                               bool waitForExit = true,
                               Func<ICmd, Task>? onStarted = null)
    {
        Output.Clear();
        ErrOut.Clear();
        SetProc(args, verb);
        StartProc();
        Stopwatch.Restart();

        if (StartInfo.RedirectStandardOutput)
            Proc.BeginOutputReadLine();

        if (StartInfo.RedirectStandardError)
            Proc.BeginErrorReadLine();

        await OnStartedAsync(waitForExit, onStarted);

        Stopwatch.Stop();

        if (waitForExit)
            Code = Proc.ExitCode;
    }

    /// <summary>Starts the process synchronously, collecting its output, and optionally waits for it to exit.</summary>
    /// <param name="args">The command-line arguments; prefixed with <c>/C</c> when the executable is <c>cmd</c>.</param>
    /// <param name="verb">An optional shell verb to apply to the start information.</param>
    /// <param name="waitForExit">Whether to wait for the process to exit and record its exit code.</param>
    /// <param name="onStarted">An optional callback invoked after the process has started.</param>
    public void Run(string args, string? verb = null, bool waitForExit = true, Action<ICmd>? onStarted = null)
    {
        Output.Clear();
        ErrOut.Clear();
        SetProc(args, verb);
        StartProc();
        Stopwatch.Restart();

        if (StartInfo.RedirectStandardOutput)
            Proc.BeginOutputReadLine();

        if (StartInfo.RedirectStandardError)
            Proc.BeginErrorReadLine();

        OnStarted(waitForExit, onStarted);

        Stopwatch.Stop();

        if (waitForExit)
            Code = Proc.ExitCode;
    }

    /// <summary>Invokes the started callback and then waits for the process to exit when requested.</summary>
    /// <param name="waitForExit">Whether to block until the process exits.</param>
    /// <param name="onStarted">An optional callback invoked with this instance.</param>
    protected virtual void OnStarted(bool waitForExit, Action<ICmd>? onStarted)
    {
        onStarted?.Invoke(this);

        if (waitForExit)
            Proc.WaitForExit();
    }

    /// <summary>Awaits the started callback and then waits for the process to exit when requested.</summary>
    /// <param name="waitForExit">Whether to wait until the process exits.</param>
    /// <param name="onStarted">An optional callback awaited with this instance.</param>
    /// <returns>A task that completes when the callback and any requested wait have finished.</returns>
    protected virtual async Task OnStartedAsync(bool waitForExit, Func<ICmd, Task>? onStarted)
    {
        if (onStarted is not null)
            await onStarted(this);

        if (waitForExit)
#if NETSTANDARD
            Proc.WaitForExit();
#else
            await Proc.WaitForExitAsync();
#endif
    }

    /// <summary>Subscribes the output and error handlers to the process data events.</summary>
    protected virtual void AttachToOutput()
    {
        Proc.OutputDataReceived += OutputHandler;
        Proc.ErrorDataReceived += ErrOutHandler;
    }

    /// <summary>Disposes any previous process, then creates, wires up and starts a new one from <see cref="StartInfo"/>.</summary>
    protected virtual void StartProc()
    {
        Proc?.Dispose();
        Proc = new();
        AttachToOutput();
        Proc.StartInfo = StartInfo;
        Proc.Start();
    }

    /// <summary>Applies the arguments, and the verb when given, to the start information.</summary>
    /// <param name="args">The command-line arguments; prefixed with <c>/C</c> when the executable is <c>cmd</c>.</param>
    /// <param name="verb">The shell verb to set, or <see langword="null"/> to leave it unchanged.</param>
    protected void SetProc(string args, string? verb)
    {
        if (StartInfo.FileName == "cmd")
            StartInfo.Arguments = "/C " + args;
        else
            StartInfo.Arguments = args;

        if (verb is not null)
            StartInfo.Verb = verb;
    }

    /// <summary>Appends a non-empty line of standard error output to <see cref="ErrOut"/>.</summary>
    /// <param name="sender">The process that raised the event.</param>
    /// <param name="outLine">The received data.</param>
    protected void ErrOutHandler(object sender, DataReceivedEventArgs outLine)
    {
        if (outLine.Data.IsNotNullOrEmptyString())
            // Add the text to the collected output.
            ErrOut.AppendLine(outLine.Data);
    }

    /// <summary>Appends a non-empty line of standard output to <see cref="Output"/>.</summary>
    /// <param name="sender">The process that raised the event.</param>
    /// <param name="outLine">The received data.</param>
    protected void OutputHandler(object sender, DataReceivedEventArgs outLine)
    {
        // Collect the sort command output.
        if (outLine.Data.IsNotNullOrEmptyString())
            // Add the text to the collected output.
            Output.AppendLine(outLine.Data);
    }

    /// <summary>Releases unmanaged resources when the instance is finalized.</summary>
    ~Cmd()
    {
        Dispose(false);
    }

    #region IDisposable
    /// <summary>Releases the process and detaches the output handlers.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the process and detaches the output handlers when disposing.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>; <see langword="false"/> when called from the finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing && Proc is not null)
        {
            Proc.OutputDataReceived -= OutputHandler;
            Proc.ErrorDataReceived -= ErrOutHandler;
            Proc?.Dispose();
        }

        _isDisposed = true;
    }
    #endregion
}