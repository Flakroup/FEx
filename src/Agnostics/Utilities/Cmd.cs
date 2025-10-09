using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace FEx.Agnostics.Utilities;

public class Cmd : ICmd
{
    private bool _isDisposed;

    public ProcessStartInfo StartInfo { get; protected set; }
    public Process Proc { get; protected set; }
    public int? Code { get; protected set; }
    public StringBuilder Output { get; }
    public StringBuilder ErrOut { get; }
    public Stopwatch Stopwatch { get; }

    public Cmd(string procName = "cmd",
               ProcessWindowStyle windowStyle = ProcessWindowStyle.Hidden,
               Action<ProcessStartInfo> cfg = null)
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

    protected Cmd()
    {
        Code = null;
        Output = new(string.Empty);
        ErrOut = new(string.Empty);
        Stopwatch = new();
    }

    public async Task RunAsync(string args,
                               string verb = null,
                               bool waitForExit = true,
                               Func<ICmd, Task> onStarted = null)
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

    public void Run(string args, string verb = null, bool waitForExit = true, Action<ICmd> onStarted = null)
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

    protected virtual void OnStarted(bool waitForExit, Action<ICmd> onStarted)
    {
        onStarted?.Invoke(this);

        if (waitForExit)
            Proc.WaitForExit();
    }

    protected virtual async Task OnStartedAsync(bool waitForExit, Func<ICmd, Task> onStarted)
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

    protected virtual void AttachToOutput()
    {
        Proc.OutputDataReceived += OutputHandler;
        Proc.ErrorDataReceived += ErrOutHandler;
    }

    protected virtual void StartProc()
    {
        Proc = new();
        AttachToOutput();
        Proc.StartInfo = StartInfo;
        Proc.Start();
    }

    protected void SetProc(string args, string verb)
    {
        if (StartInfo.FileName == "cmd")
            StartInfo.Arguments = "/C " + args;
        else
            StartInfo.Arguments = args;

        if (verb is not null)
            StartInfo.Verb = verb;
    }

    protected void ErrOutHandler(object sender, DataReceivedEventArgs outLine)
    {
        if (outLine.Data.IsNotNullOrEmptyString())
            // Add the text to the collected output.
            ErrOut.AppendLine(outLine.Data);
    }

    protected void OutputHandler(object sender, DataReceivedEventArgs outLine)
    {
        // Collect the sort command output.
        if (outLine.Data.IsNotNullOrEmptyString())
            // Add the text to the collected output.
            Output.AppendLine(outLine.Data);
    }

    ~Cmd()
    {
        Dispose(false);
    }

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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