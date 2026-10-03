using FEx.Common.Startup;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Common.Tests;

public class StartupHostTests
{
    private readonly RecordingHost _host = new();

    [Fact]
    public async Task Container_init_is_awaited_not_blocked_on()
    {
        var gate = new TaskCompletionSource<bool>();
        _host.InitGate = gate.Task;

        var run = _host.RunAsync();

        // Control returns to the caller while the container is still building; no hook ran yet.
        run.IsCompleted.ShouldBeFalse();
        _host.Events.ShouldBe(["Prepare", "Suspend", "ShowStartupWindow", "Init"]);

        gate.SetResult(true);
        await run;

        _host.Events.ShouldContain("OnActivation");
    }

    [Fact]
    public async Task Steps_run_in_order_after_the_container_exists()
    {
        await _host.RunAsync();

        _host.Events.ShouldBe(
        [
            "Prepare", "Suspend", "ShowStartupWindow", "Init", "CloseStartupWindow", "Restore",
            "PublishServices", "OnActivation", "EnsureSingleInstance", "InitializeComponents", "BeforeStartup",
            "AfterServicesContainerBuild", "ShowMainWindow", "AfterStartup", "ExitIfFailed"
        ]);
        _host.ExitCodes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Main_window_is_shown_after_an_init_that_really_yields()
    {
        _host.InitGate = Task.Delay(30, TestContext.Current.CancellationToken);

        await _host.RunAsync();

        _host.Events.IndexOf("Init").ShouldBeLessThan(_host.Events.IndexOf("ShowMainWindow"));
        _host.Events.IndexOf("AfterServicesContainerBuild").ShouldBeLessThan(_host.Events.IndexOf("ShowMainWindow"));
    }

    [Fact]
    public async Task Shutdown_is_restored_before_hooks_so_a_hook_set_mode_survives()
    {
        await _host.RunAsync();

        var restore = _host.Events.IndexOf("Restore");
        restore.ShouldBeGreaterThan(_host.Events.IndexOf("CloseStartupWindow"));
        restore.ShouldBeLessThan(_host.Events.IndexOf("InitializeComponents"));
    }

    [Fact]
    public async Task Failed_init_closes_the_startup_window_restores_shutdown_and_exits_non_zero()
    {
        var failure = new InvalidOperationException("boom");
        _host.InitGate = Task.FromException(failure);

        await _host.RunAsync();

        _host.Handled.ShouldBe([failure]);
        _host.ExitCodes.ShouldBe([1]);
        _host.Events.ShouldBe(["Prepare", "Suspend", "ShowStartupWindow", "Init", "CloseStartupWindow", "Restore"]);
    }

    [Fact]
    public async Task Hook_exception_stops_the_flow_and_exits_non_zero()
    {
        var failure = new InvalidOperationException("boom");
        _host.FailAt = "BeforeStartup";
        _host.Failure = failure;

        await _host.RunAsync();

        _host.Handled.ShouldBe([failure]);
        _host.ExitCodes.ShouldBe([1]);
        _host.Events.ShouldNotContain("ShowMainWindow");
    }

    [Fact]
    public async Task Rejected_configuration_never_shows_a_window_or_builds_the_container()
    {
        var failure = new InvalidOperationException("StartupUri");
        _host.FailAt = "Prepare";
        _host.Failure = failure;

        await _host.RunAsync();

        _host.Handled.ShouldBe([failure]);
        _host.ExitCodes.ShouldBe([1]);
        _host.Events.ShouldBe(["Prepare"]);
    }

    private sealed class RecordingHost : StartupHost
    {
        public List<string> Events { get; } = [];
        public List<Exception> Handled { get; } = [];
        public List<int> ExitCodes { get; } = [];
        public Task InitGate { get; set; } = Task.CompletedTask;
        public string? FailAt { get; set; }
        public Exception? Failure { get; set; }

        private void Step(string name)
        {
            Events.Add(name);
            if (name == FailAt)
                throw Failure!;
        }

        protected override void PrepareStartup() => Step("Prepare");
        protected override void SuspendShutdown() => Step("Suspend");
        protected override void ShowStartupWindow() => Step("ShowStartupWindow");

        protected override async Task InitializeContainerAsync()
        {
            Events.Add("Init");
#pragma warning disable VSTHRD003 // the gate is owned by the test
            await InitGate;
#pragma warning restore VSTHRD003
        }

        protected override void CloseStartupWindow() => Step("CloseStartupWindow");
        protected override void RestoreShutdown() => Step("Restore");
        protected override void PublishServices() => Step("PublishServices");
        protected override void OnActivation() => Step("OnActivation");
        protected override void EnsureSingleInstance() => Step("EnsureSingleInstance");
        protected override void InitializeComponents() => Step("InitializeComponents");
        protected override void BeforeStartup() => Step("BeforeStartup");
        protected override void AfterServicesContainerBuild() => Step("AfterServicesContainerBuild");
        protected override void ShowMainWindow() => Step("ShowMainWindow");
        protected override void AfterStartup() => Step("AfterStartup");
        protected override void ExitIfInitializationHasFailed() => Step("ExitIfFailed");
        protected override void HandleException(Exception exception) => Handled.Add(exception);
        protected override void RequestExit(int exitCode) => ExitCodes.Add(exitCode);
    }
}
