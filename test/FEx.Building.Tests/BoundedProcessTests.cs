using Nuke.Common.Tooling;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// A timeout that kills only the root leaves the test host and the compiler server holding the binaries, so
/// the wedge the timeout exists to remove survives it. These pin that expiry kills through the process
/// handed in - a <see cref="ProcessTree" /> in production - and fails the target with a message that names
/// the bound.
/// </summary>
public sealed class BoundedProcessTests
{
    [Fact]
    public void AProcessThatFinishesInTime_ReturnsItsOutput()
    {
        var state = new State(exitCode: 0);
        state.Exited.Set();

        BoundedProcess.Run(() => new FakeProcess(state), TimeSpan.FromSeconds(30), "The run").ShouldBeEmpty();

        state.Killed.ShouldBeFalse();
    }

    [Fact]
    public void AProcessThatOutlivesTheTimeout_IsKilled_AndTheRunFailsNamingTheBound()
    {
        var state = new State(exitCode: -1);

        var failure = Should.Throw<TimeoutException>(() =>
            BoundedProcess.Run(() => new FakeProcess(state), TimeSpan.FromMilliseconds(50), "The test run"));

        state.Killed.ShouldBeTrue();
        failure.Message.ShouldContain("The test run");
        failure.Message.ShouldContain("00:00:00.0500000");
        failure.Message.ShouldContain("process tree was killed");
    }

    [Fact]
    public void ANullTimeout_WaitsForTheProcessWithoutKillingIt()
    {
        var state = new State(exitCode: 0);
        using var timer = new Timer(_ => state.Exited.Set(), null, 100, Timeout.Infinite);

        BoundedProcess.Run(() => new FakeProcess(state), timeout: null, "The run");

        state.Killed.ShouldBeFalse();
    }

    [Fact]
    public void ANonZeroExit_StillFailsTheRun()
    {
        var state = new State(exitCode: 1);
        state.Exited.Set();

        Should.Throw<Exception>(() => BoundedProcess.Run(() => new FakeProcess(state), TimeSpan.FromSeconds(30), "The run"));
    }

    [Fact]
    public void Validate_TreatsNullAndInfiniteAsNoTimeout()
    {
        BoundedProcess.Validate(null, "T").ShouldBeNull();
        BoundedProcess.Validate(Timeout.InfiniteTimeSpan, "T").ShouldBeNull();
        BoundedProcess.Validate(TimeSpan.FromMinutes(3), "T").ShouldBe(TimeSpan.FromMinutes(3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    [InlineData(-2)]
    public void Validate_RejectsZeroAndOtherNegativeValues_RatherThanClampingThem(int milliseconds)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => BoundedProcess.Validate(TimeSpan.FromMilliseconds(milliseconds), "T"));
    }

    [Fact]
    public void Validate_RejectsAValueTheWaitCannotTake()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BoundedProcess.Validate(TimeSpan.FromDays(60), "T"));
    }

    /// <summary>What the test observes of the process, which the run under test disposes.</summary>
    private sealed class State(int exitCode)
    {
        public int ExitCode { get; } = exitCode;
        public ManualResetEventSlim Exited { get; } = new();
        public bool Killed { get; set; }
    }

    /// <summary>Blocks in <see cref="WaitForExit" /> until finished or killed, like a hung build.</summary>
    private sealed class FakeProcess(State state) : IProcess
    {
        public string FileName => "dotnet";
        public string Arguments => "";
        public string WorkingDirectory => "";
        public IReadOnlyCollection<Output> Output { get; } = [];
        public int ExitCode => state.ExitCode;
        public bool HasExited => state.Exited.IsSet;
        public int Id => 0;

        public void Kill()
        {
            state.Killed = true;
            state.Exited.Set();
        }

        public bool WaitForExit()
        {
            state.Exited.Wait();

            return true;
        }

        public void Dispose()
        {
        }
    }
}
