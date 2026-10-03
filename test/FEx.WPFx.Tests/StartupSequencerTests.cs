using FEx.AppStartup;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FEx.WPFx.Tests;

public class StartupSequencerTests
{
    private readonly List<string> _events = [];
    private readonly List<Exception> _handled = [];
    private readonly List<int> _exitCodes = [];

    private Task Run(Func<Task> initialize, params Action[] hooks) =>
        StartupSequencer.RunAsync(() => _events.Add("show"),
                                  initialize,
                                  () => _events.Add("close"),
                                  hooks,
                                  _handled.Add,
                                  _exitCodes.Add);

    [Fact]
    public async Task Container_init_is_awaited_not_blocked_on()
    {
        var gate = new TaskCompletionSource<bool>();

#pragma warning disable VSTHRD003 // the gate is owned by this test
        var run = Run(() => gate.Task, () => _events.Add("hook"));
#pragma warning restore VSTHRD003

        // Returns control to the caller while the container is still building.
        run.IsCompleted.ShouldBeFalse();
        _events.ShouldBe(["show"]);

        gate.SetResult(true);
        await run;

        _events.ShouldBe(["show", "close", "hook"]);
    }

    [Fact]
    public async Task Hooks_run_in_order_after_the_container_exists()
    {
        var containerBuilt = false;

        await Run(() =>
                  {
                      containerBuilt = true;
                      return Task.CompletedTask;
                  },
                  () => _events.Add($"first:{containerBuilt}"),
                  () => _events.Add($"second:{containerBuilt}"),
                  () => _events.Add($"third:{containerBuilt}"));

        _events.ShouldBe(["show", "close", "first:True", "second:True", "third:True"]);
        _exitCodes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Startup_window_is_shown_before_init_and_closed_after()
    {
        await Run(() =>
        {
            _events.Add("init");
            return Task.CompletedTask;
        });

        _events.ShouldBe(["show", "init", "close"]);
    }

    [Fact]
    public async Task Init_exception_reaches_the_handler_and_requests_a_non_zero_exit()
    {
        var failure = new InvalidOperationException("boom");

        await Run(() => Task.FromException(failure), () => _events.Add("hook"));

        _handled.ShouldBe([failure]);
        _exitCodes.ShouldBe([1]);
        _events.ShouldBe(["show", "close"]);
    }

    [Fact]
    public async Task Hook_exception_stops_the_sequence_and_requests_a_non_zero_exit()
    {
        var failure = new InvalidOperationException("boom");

        await Run(() => Task.CompletedTask,
                  () => throw failure,
                  () => _events.Add("never"));

        _handled.ShouldBe([failure]);
        _exitCodes.ShouldBe([1]);
        _events.ShouldNotContain("never");
    }
}
