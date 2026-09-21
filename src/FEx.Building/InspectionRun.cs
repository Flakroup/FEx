using FEx.Building.Helpers;
using Microsoft.VisualStudio.Threading;
using Nuke.Common.Tooling;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FEx.Building;

/// <summary>
/// One execution of the inspection - the tool restore, the tool itself, the report read into findings -
/// that can be started ahead of the target that collects its verdict.
/// </summary>
/// <remarks>
/// The inspection is the longest step of a build it shares with the suite, and the two do not read what
/// the other writes: both consume the tree Compile left. Started right after Compile it runs alongside the
/// tests, and the build ends when the longer of the two does instead of when their sum does. A build that
/// never reaches the collecting target - a red suite - <see cref="Discard" />s the run instead.
/// <para>
/// Output is not streamed. The tool writes into a buffer that is replayed when the verdict is collected,
/// so its lines land in the block of the target that reads them rather than interleaved with the suite's.
/// </para>
/// </remarks>
public sealed class InspectionRun
{
    private readonly string _arguments;
    private readonly Func<string> _readReport;
    private readonly Func<string, IProcess> _startDotNet;
    private readonly List<Output> _output = [];
    private readonly Stopwatch _running = new();
    private readonly object _gate = new();
    private JoinableTask<IReadOnlyList<InspectionFinding>>? _pending;
    private IProcess? _current;
    private bool _discarded;

    /// <param name="arguments">The inspection command line, as <see cref="IInspectTarget.InspectionArguments" /> composes it.</param>
    /// <param name="readReport">Hands over the report the tool wrote, once it has exited.</param>
    /// <param name="startDotNet">
    /// Starts <c>dotnet</c> with the given arguments and returns without waiting - the seam that lets a test
    /// stand in for the tool.
    /// </param>
    public InspectionRun(string arguments, Func<string> readReport, Func<string, IProcess> startDotNet)
    {
        _arguments = arguments;
        _readReport = readReport;
        _startDotNet = startDotNet;
    }

    /// <summary>
    /// The run not yet collected - one per build process, since a build is one process and starts the
    /// inspection once. Set by <see cref="StartInBackground" />, or by <see cref="Collect" /> when it runs
    /// the inspection inline; cleared by <see cref="Collect" /> and <see cref="DiscardPending" />.
    /// </summary>
    public static InspectionRun? Pending { get; private set; }

    /// <summary>Starts the inspection on the thread pool, publishes it as <see cref="Pending" /> and returns at once.</summary>
    public void StartInBackground()
    {
        // Published before the work starts: a discard racing the first process would otherwise find
        // nothing pending and leave the run to start its next one.
        Pending = this;
        _pending = JoinableTaskHelper.RunAsync(() => Task.Run(Execute));
    }

    /// <summary>
    /// Kills the <see cref="Pending" /> run's tool if it is still running - the build ended before anything
    /// could read its verdict. Wired into <see cref="FExBuild.OnBuildFinished" />.
    /// </summary>
    /// <returns>Whether a process was killed.</returns>
    public static bool DiscardPending()
    {
        var discarded = Pending?.Discard() ?? false;
        Pending = null;

        return discarded;
    }

    /// <summary>
    /// The findings, once the inspection is over: joins the background run when one was started, runs
    /// the whole inspection inline otherwise. Either way the tool's output is replayed here, failure
    /// included, so what it said is on the screen before the exception that quotes its exit code.
    /// </summary>
    public IReadOnlyList<InspectionFinding> Collect()
    {
        var waited = Stopwatch.StartNew();

        // An inline run is published too: it is the default now that the background start is opt-in, and
        // the end-of-build hook can only kill what it can find.
        if (_pending is null)
            Pending = this;

        try
        {
            return _pending?.Join() ?? Execute();
        }
        finally
        {
            // Cleared AFTER the join, not before it: a Ctrl+C while Inspect waits ends the build from
            // another thread, and the run it should kill is this one, still pending.
            if (Pending == this)
                Pending = null;

            foreach (var line in _output)
                ProcessTasks.DefaultLogger(line.Type, line.Text);

            if (_pending is not null)
                Log.Information("Inspection ran {Ran:F0}s alongside the build, {Waited:F0}s of it after Inspect was reached",
                    _running.Elapsed.TotalSeconds,
                    waited.Elapsed.TotalSeconds);
        }
    }

    /// <summary>
    /// Stops a run nobody will read: kills the tool if it is still running, and refuses to start the next
    /// process if the run is between two. Idempotent; a run that has already finished is left alone.
    /// </summary>
    /// <remarks>
    /// Serialized with the start of each process: a discard cannot land between the check that lets a
    /// process start and the moment it becomes the one to kill, and it never sees a process that
    /// <see cref="Run" /> has already disposed.
    /// </remarks>
    /// <returns>Whether a process was killed.</returns>
    internal bool Discard()
    {
        lock (_gate)
        {
            _discarded = true;

            if (_current is not { HasExited: false } process)
                return false;

            try
            {
                process.Kill();
            }
            catch (InvalidOperationException)
            {
                // Exited between the check and the kill - nothing left to stop.
                return false;
            }
        }

        Log.Warning("Inspection discarded: the build ended before Inspect could read its verdict");

        return true;
    }

    private IReadOnlyList<InspectionFinding> Execute()
    {
        _running.Start();

        try
        {
            Run("tool restore");
            Run(_arguments);

            return InspectionGate.Analyze(_readReport());
        }
        finally
        {
            _running.Stop();
        }
    }

    private void Run(string arguments)
    {
        IProcess process;

        lock (_gate)
        {
            if (_discarded)
                throw new OperationCanceledException("The inspection was discarded before this step could start");

            process = _startDotNet(arguments);
            _current = process;
        }

        using (process)
        {
            // The invocation line NUKE would have logged at the start, buffered with the output so both
            // land in the block that reads them rather than in whichever target was running at the time.
            _output.Add(new Output { Type = OutputType.Std, Text = $"> dotnet {arguments}" });
            process.WaitForExit();

            // Cleared before the process is disposed: Discard asks the process it finds here whether it has
            // exited, and a disposed one answers with an exception.
            lock (_gate)
                _current = null;

            _output.AddRange(process.Output);
            process.AssertZeroExitCode();
        }
    }
}
