using FEx.Building.Helpers;
using Nuke.Common.Tooling;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The inspection as a run that starts before the target that reads it. Each assertion here is a way the
/// early start can silently turn back into the old sequential run - or into a tool nobody stops.
/// </summary>
/// <remarks>
/// One class on purpose: <see cref="InspectionRun.Pending" /> is process-wide state, and xUnit runs the
/// tests of one class one after another.
/// </remarks>
[Collection(GlobalLoggerCollection.Name)]
public sealed class InspectionRunTests
{
    private const string CleanReport = """
        { "runs": [ { "invocations": [ { "executionSuccessful": true } ],
          "tool": { "driver": {} }, "results": [] } ] }
        """;

    private const string InspectArguments = "jb inspectcode \"/repo/My Solution.slnx\"";

    [Fact]
    public void StartInBackground_ReturnsBeforeTheToolHasExited()
    {
        using var tool = new FakeDotNet(holdTheInspection: true);
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();

        tool.InspectionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
        tool.Inspection!.HasExited.ShouldBeFalse();

        tool.ReleaseTheInspection();
        run.Collect().ShouldBeEmpty();
    }

    [Fact]
    public void Collect_JoinsThePendingRun_RatherThanStartingAnother()
    {
        // The regression the whole feature turns into if this breaks: two inspections per build, the
        // early one wasted and the late one costing exactly what it cost before.
        using var tool = new FakeDotNet();
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();
        run.Collect();

        tool.Started.ShouldBe(["tool restore", InspectArguments]);
    }

    [Fact]
    public void Collect_WithNothingPending_RunsTheInspectionInline()
    {
        // `Inspect --skip TriggerInspect` - the run has to happen somewhere, and it happens here.
        using var tool = new FakeDotNet();
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.Collect().ShouldBeEmpty();

        tool.Started.ShouldBe(["tool restore", InspectArguments]);
    }

    [Fact]
    public void TheToolRestore_RunsBeforeTheInspection()
    {
        // The manifest pins the tool; without the restore `dotnet jb` is "command not found" on a fresh
        // checkout, and on a background thread that failure would surface minutes later, at the join.
        using var tool = new FakeDotNet();

        new InspectionRun(InspectArguments, static () => CleanReport, tool.Start).Collect();

        tool.Started.First().ShouldBe("tool restore");
    }

    [Fact]
    public void AToolThatFails_FailsTheCollect()
    {
        // logOutput is off for the background process, so an exit code swallowed here would be a
        // failure nobody sees - the report would just be missing or stale.
        using var tool = new FakeDotNet(exitCode: 1);
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();

        Should.Throw<ProcessException>(run.Collect);
    }

    [Fact]
    public void Collect_ReadsTheReportOnlyAfterTheToolHasExited()
    {
        using var tool = new FakeDotNet(holdTheInspection: true);
        var readWhileRunning = false;
        var run = new InspectionRun(InspectArguments,
            () =>
            {
                readWhileRunning = !tool.Inspection!.HasExited;

                return CleanReport;
            },
            tool.Start);

        run.StartInBackground();
        tool.InspectionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
        tool.ReleaseTheInspection();
        run.Collect();

        readWhileRunning.ShouldBeFalse();
    }

    [Fact]
    public void DiscardPending_KillsATool_StillRunningWhenTheBuildEnds()
    {
        // A red suite ends the build before Inspect. Without this, the process that would have read the
        // verdict is gone and the tool keeps every core busy for minutes on a report nobody will open.
        using var tool = new FakeDotNet(holdTheInspection: true);
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();
        tool.InspectionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();

        InspectionRun.DiscardPending().ShouldBeTrue();

        tool.Inspection!.Killed.ShouldBeTrue();
        InspectionRun.Pending.ShouldBeNull();
    }

    [Fact]
    public async Task DiscardPending_KillsAnInlineRun_StillGoingWhenTheBuildEnds()
    {
        // Ctrl+C, or a cancelled CI job, while Inspect runs the whole inspection itself - the default since
        // the background start is opt-in. An unpublished run is invisible to the hook, so the tool outlived the build.
        using var tool = new FakeDotNet(holdTheInspection: true);
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        var collecting = Task.Run(run.Collect, TestContext.Current.CancellationToken);
        tool.InspectionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();

        InspectionRun.DiscardPending().ShouldBeTrue();

        tool.Inspection!.Killed.ShouldBeTrue();
        await Should.ThrowAsync<ProcessException>(collecting);
        InspectionRun.Pending.ShouldBeNull();
    }

    [Fact]
    public void DiscardPending_LeavesAFinishedRunAlone()
    {
        using var tool = new FakeDotNet();
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();
        run.Collect();

        InspectionRun.DiscardPending().ShouldBeFalse();
        tool.Inspection!.Killed.ShouldBeFalse();
    }

    [Fact]
    public void DiscardPending_WithNothingPending_IsANoOp()
    {
        InspectionRun.DiscardPending().ShouldBeFalse();
    }

    [Fact]
    public void ADiscardedRun_DoesNotStartItsNextProcess()
    {
        // The discard can land between the restore and the inspection. Killing "the current process" then
        // kills nothing - the restore has exited - and the inspection would start a moment later as an orphan.
        using var tool = new FakeDotNet();
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);
        tool.OnRestoreExited = static () => InspectionRun.DiscardPending().ShouldBeFalse();

        run.StartInBackground();

        Should.Throw<OperationCanceledException>(run.Collect);
        tool.Started.ShouldBe(["tool restore"]);
    }

    [Fact]
    public async Task ADiscard_LandingWhileTheToolIsBeingStarted_StillReachesIt()
    {
        // Starting a process takes tens of milliseconds, and a discard in that window used to find nothing
        // to kill - the check had passed, the process was not yet the current one - and let it run on.
        using var tool = new FakeDotNet(holdTheInspection: true);
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);
        Task<bool>? discard = null;
        tool.OnInspectionStarting = () => discard = Task.Run(InspectionRun.DiscardPending);

        run.StartInBackground();

        tool.InspectionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
        (await discard!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).ShouldBeTrue();
        tool.Inspection!.Killed.ShouldBeTrue();
        Should.Throw<ProcessException>(run.Collect);
    }

    [Fact]
    public void Collect_ReplaysEachInvocation_FollowedByWhatItSaid()
    {
        // The background thread logs nothing while it runs; the whole account of the tool is what this
        // replay prints, so a dropped line here is a line nobody ever sees.
        using var tool = new FakeDotNet();
        var sink = new CapturingSink();
        var previous = Log.Logger;
        Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(sink).CreateLogger();

        try
        {
            new InspectionRun(InspectArguments, static () => CleanReport, tool.Start).Collect();
        }
        finally
        {
            Log.Logger = previous;
        }

        sink.Messages.Where(static message => message.StartsWith("> dotnet", StringComparison.Ordinal) || message == "line")
            .ShouldBe(["> dotnet tool restore", "line", $"> dotnet {InspectArguments}", "line"]);
    }

    [Fact]
    public void Collect_ClearsThePendingRun()
    {
        // Otherwise the end of the build would "discard" a run it has already read - harmless today, as a
        // finished process is left alone, but a second Inspect in one build would then re-collect a spent run.
        using var tool = new FakeDotNet();
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();
        InspectionRun.Pending.ShouldBe(run);

        run.Collect();

        InspectionRun.Pending.ShouldBeNull();
    }

    [Fact]
    public void TheEndOfTheBuild_DiscardsThePendingRun()
    {
        // The hook, not just the method: a build that forgets to call it leaves the orphan.
        using var tool = new FakeDotNet(holdTheInspection: true);
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();
        tool.InspectionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();

        new InspectingBuild().Finish();

        tool.Inspection!.Killed.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(ExitedRaces))]
    public void ADiscard_ThatLosesTheRaceWithTheToolsExit_ReportsNothingKilled(Exception race)
    {
        // The tool exits between the discard's check and its kill. The discard runs from the end-of-build
        // hook, in a finally: an exception here would replace the red test the developer needs to see.
        using var tool = new FakeDotNet(holdTheInspection: true) { KillFailure = race, ExitsWhenKillFails = true };
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();
        tool.InspectionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();

        InspectionRun.DiscardPending().ShouldBeFalse();
        run.Collect().ShouldBeEmpty();
    }

    public static TheoryData<Exception> ExitedRaces() =>
    [
        new InvalidOperationException("ProcessTree: the root has already exited"),
        new ArgumentException("Process.GetProcessById: the id is no longer running"),
    ];

    [Fact]
    public void ADiscard_ThatLosesTheRaceInsideTheTreeKill_ReportsNothingKilled()
    {
        // The real ProcessTree and the real runtime: the root is still running when both checks read it and
        // gone by the time the tree kill looks its id up, which is when GetProcessById throws.
        using var tool = new FakeDotNet(holdTheInspection: true) { ExitsWhenAimedAt = true, AsTree = true };
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();
        tool.InspectionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();

        InspectionRun.DiscardPending().ShouldBeFalse();
        run.Collect().ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(RefusedKills))]
    public void ADiscard_ThatCannotKillAToolStillRunning_Surfaces(Exception refusal)
    {
        // Access denied, or a tree the runtime will not kill: the tool keeps running after the build, and
        // that is worth an exception - swallowed, it would read exactly like "nothing left to stop".
        using var tool = new FakeDotNet(holdTheInspection: true) { KillFailure = refusal };
        var run = new InspectionRun(InspectArguments, static () => CleanReport, tool.Start);

        run.StartInBackground();
        tool.InspectionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();

        Should.Throw<Exception>(static () => InspectionRun.DiscardPending()).ShouldBeSameAs(refusal);

        tool.ReleaseTheInspection();
        run.Collect().ShouldBeEmpty();
    }

    public static TheoryData<Exception> RefusedKills() =>
    [
        new Win32Exception(5, "Access is denied"),
        new AggregateException(new Win32Exception(5, "Access is denied")),
        new InvalidOperationException("Cannot kill a tree that contains the calling process"),
        new ArgumentException("A live process refused for a reason other than its exit"),
    ];

    [Fact]
    public void RunAsync_StartsTheWork_AndJoinReturnsItsResult()
    {
        var joinable = JoinableTaskHelper.RunAsync(static () => Task.FromResult(42));

        joinable.Join(TestContext.Current.CancellationToken).ShouldBe(42);
    }

    private sealed class InspectingBuild : FExBuild, IInspectTarget
    {
        public override IEnumerable<string> PublishProjects { get; } = [];

        public void Finish() => OnBuildFinished();
    }

    /// <summary>
    /// Stands in for <c>dotnet</c>: records what it was asked to start, and can hold a step open until the
    /// test releases it - the only way to observe "returned before the tool exited".
    /// </summary>
    private sealed class FakeDotNet : IDisposable
    {
        private readonly bool _holdTheInspection;
        private readonly int _exitCode;
        private readonly ManualResetEventSlim _inspectionMayExit = new(false);

        public FakeDotNet(bool holdTheInspection = false, int exitCode = 0)
        {
            _holdTheInspection = holdTheInspection;
            _exitCode = exitCode;
        }

        public List<string> Started { get; } = [];
        public ManualResetEventSlim InspectionStarted { get; } = new(false);
        public FakeProcess? Inspection { get; private set; }
        public Action? OnRestoreExited { get; set; }
        public Action? OnInspectionStarting { get; set; }
        public Exception? KillFailure { get; init; }
        public bool ExitsWhenKillFails { get; init; }
        public bool ExitsWhenAimedAt { get; init; }
        public bool AsTree { get; init; }

        public void ReleaseTheInspection() => _inspectionMayExit.Set();

        public IProcess Start(string arguments)
        {
            Started.Add(arguments);

            if (arguments == "tool restore")
                return new FakeProcess(arguments, null, 0, OnRestoreExited);

            OnInspectionStarting?.Invoke();
            Inspection?.Dispose();
            Inspection = new FakeProcess(arguments, _holdTheInspection ? _inspectionMayExit : null, _exitCode, null)
            {
                KillFailure = KillFailure,
                ExitsWhenKillFails = ExitsWhenKillFails,
                ExitsWhenAimedAt = ExitsWhenAimedAt,
            };
            InspectionStarted.Set();

            return AsTree ? new ProcessTree(Inspection) : Inspection;
        }

        public void Dispose()
        {
            _inspectionMayExit.Dispose();
            InspectionStarted.Dispose();
            Inspection?.Dispose();
        }
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public List<string> Messages { get; } = [];

        public void Emit(LogEvent logEvent) => Messages.Add(logEvent.RenderMessage());
    }

    private sealed class FakeProcess : IProcess
    {
        private readonly ManualResetEventSlim? _mayExit;
        private readonly int _exitCode;
        private readonly Action? _onExited;

        public FakeProcess(string arguments, ManualResetEventSlim? mayExit, int exitCode, Action? onExited)
        {
            Arguments = arguments;
            _mayExit = mayExit;
            _exitCode = exitCode;
            _onExited = onExited;
        }

        public bool Killed { get; private set; }
        public Exception? KillFailure { get; init; }
        public bool ExitsWhenKillFails { get; init; }

        /// <summary>
        /// Exits the moment something reads the id to aim a kill at it, and answers with one no process
        /// holds - the window between a tree kill's exit check and its lookup by id.
        /// </summary>
        public bool ExitsWhenAimedAt { get; init; }

        public string FileName => "dotnet";
        public string Arguments { get; }
        public string WorkingDirectory => "/repo";
        public IReadOnlyCollection<Output> Output { get; } = [new Output { Type = OutputType.Std, Text = "line" }];
        public int ExitCode => Killed ? -1 : _exitCode;
        public bool HasExited { get; private set; }
        public int Id
        {
            get
            {
                if (!ExitsWhenAimedAt)
                    return 4242;

                Exit();

                return int.MaxValue;
            }
        }

        public void Kill()
        {
            if (KillFailure is not null)
            {
                if (ExitsWhenKillFails)
                    Exit();

                throw KillFailure;
            }

            Killed = true;
            HasExited = true;
            _mayExit?.Set();
        }

        private void Exit()
        {
            HasExited = true;
            _mayExit?.Set();
        }

        public bool WaitForExit()
        {
            _mayExit?.Wait();
            HasExited = true;
            _onExited?.Invoke();

            return true;
        }

        public void Dispose()
        {
        }
    }
}
