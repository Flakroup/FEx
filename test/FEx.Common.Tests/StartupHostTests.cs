using FEx.Common.Startup;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Common.Tests;

public class StartupHostTests
{
    private readonly FakeDesktopHost _host = new();

    [Fact]
    public async Task Container_init_is_awaited_not_blocked_on()
    {
        var gate = new TaskCompletionSource<bool>();
        _host.InitGate = gate.Task;

        var run = _host.RunAsync();

        // Control returns to the caller while the container is still building; no hook ran yet.
        run.IsCompleted.ShouldBeFalse();
        _host.Events.ShouldBe(["Suspend", "ShowStartupWindow", "Init"]);

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
            "Suspend", "ShowStartupWindow", "Init", "CloseStartupWindow", "Restore", "SetMainThread",
            "PublishServices", "OnActivation", "EnsureSingleInstance", "InitializeComponents", "BeforeStartup",
            "AfterServicesContainerBuild", "ShowMainWindow", "MainWindowShown", "AfterStartup", "ExitIfFailed"
        ]);
        _host.ExitCodes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Main_thread_is_set_right_after_the_container_and_before_any_hook()
    {
        await _host.RunAsync();

        var setMainThread = _host.Events.IndexOf("SetMainThread");
        setMainThread.ShouldBeGreaterThan(_host.Events.IndexOf("Init"));
        setMainThread.ShouldBeLessThan(_host.Events.IndexOf("PublishServices"));
    }

    [Fact]
    public async Task Main_window_is_not_created_while_init_is_pending_and_is_shown_after_it()
    {
        var gate = new TaskCompletionSource<bool>();
        _host.InitGate = gate.Task;

        var run = _host.RunAsync();
        _host.Shown.ShouldNotContain(_host.MainWindowInstance);

        gate.SetResult(true);
        await run;

        _host.Shown.ShouldContain(_host.MainWindowInstance);
        _host.MainWindowSlot.ShouldBe(_host.MainWindowInstance);
    }

    [Fact]
    public async Task Startup_window_is_shown_before_init_closed_after_and_does_not_keep_the_main_window_slot()
    {
        var gate = new TaskCompletionSource<bool>();
        _host.InitGate = gate.Task;

        var run = _host.RunAsync();

        _host.Shown.ShouldBe([_host.StartupWindowInstance]);
        _host.MainWindowSlot.ShouldBe(_host.StartupWindowInstance);
        _host.Closed.ShouldBeEmpty();

        gate.SetResult(true);
        await run;

        _host.Closed.ShouldBe([_host.StartupWindowInstance]);
        _host.MainWindowSlot.ShouldBe(_host.MainWindowInstance);
    }

    [Fact]
    public async Task Shutdown_is_explicit_while_init_runs_and_a_mode_set_by_a_hook_survives()
    {
        var gate = new TaskCompletionSource<bool>();
        _host.InitGate = gate.Task;
        _host.ModeSetByHook = FakeMode.OnMainWindowClose;

        var run = _host.RunAsync();
        _host.Mode.ShouldBe(FakeMode.Explicit);

        gate.SetResult(true);
        await run;

        _host.Mode.ShouldBe(FakeMode.OnMainWindowClose);
    }

    [Fact]
    public async Task Configured_shutdown_mode_is_restored_when_no_hook_changes_it()
    {
        _host.Mode = FakeMode.OnLastWindowClose;

        await _host.RunAsync();

        _host.Mode.ShouldBe(FakeMode.OnLastWindowClose);
    }

    [Fact]
    public async Task Failed_init_closes_the_startup_window_restores_shutdown_and_exits_non_zero()
    {
        var failure = new InvalidOperationException("boom");
        _host.InitGate = Task.FromException(failure);
        _host.Mode = FakeMode.OnLastWindowClose;

        await _host.RunAsync();

        _host.EarlyHandled.ShouldBe([failure]);
        _host.Handled.ShouldBeEmpty();
        _host.ExitCodes.ShouldBe([1]);
        _host.Closed.ShouldBe([_host.StartupWindowInstance]);
        _host.Mode.ShouldBe(FakeMode.OnLastWindowClose);
        _host.Events.ShouldNotContain("PublishServices");
    }

    [Fact]
    public async Task Startup_uri_is_rejected_before_any_window_or_container_and_reaches_the_container_free_handler()
    {
        _host.StartupUri = true;

        await _host.RunAsync();

        _host.EarlyHandled.ShouldHaveSingleItem().Message.ShouldContain("StartupUri");
        _host.Handled.ShouldBeEmpty();
        _host.ExitCodes.ShouldBe([1]);
        _host.Events.ShouldBeEmpty();
        _host.Shown.ShouldBeEmpty();
    }

    [Fact]
    public async Task Hook_failure_after_the_services_are_published_uses_the_regular_handler_and_exits_non_zero()
    {
        var failure = new InvalidOperationException("boom");
        _host.FailAt = "BeforeStartup";
        _host.Failure = failure;

        await _host.RunAsync();

        _host.Handled.ShouldBe([failure]);
        _host.EarlyHandled.ShouldBeEmpty();
        _host.ExitCodes.ShouldBe([1]);
        _host.Events.ShouldNotContain("ShowMainWindow");
    }

    private enum FakeMode
    {
        OnLastWindowClose,
        OnMainWindowClose,
        Explicit
    }

    private sealed class FakeWindow(string name)
    {
        public override string ToString() => name;
    }

    // Behaves like WPF: the first window shown takes the main window slot.
    private sealed class FakeDesktopHost : DesktopStartupHost<FakeWindow, FakeMode>
    {
        public List<string> Events { get; } = [];
        public List<Exception> Handled { get; } = [];
        public List<Exception> EarlyHandled { get; } = [];
        public List<int> ExitCodes { get; } = [];
        public List<FakeWindow> Shown { get; } = [];
        public List<FakeWindow> Closed { get; } = [];
        public FakeWindow StartupWindowInstance { get; } = new("startup");
        public FakeWindow MainWindowInstance { get; } = new("main");
        public FakeWindow? MainWindowSlot { get; private set; }
        public FakeMode Mode { get; set; } = FakeMode.OnLastWindowClose;
        public FakeMode? ModeSetByHook { get; set; }
        public bool StartupUri { get; set; }
        public Task InitGate { get; set; } = Task.CompletedTask;
        public string? FailAt { get; set; }
        public Exception? Failure { get; set; }

        protected override FakeMode ShutdownMode
        {
            get => Mode;
            set => Mode = value;
        }

        protected override FakeMode ExplicitShutdownMode => FakeMode.Explicit;

        protected override FakeWindow? MainWindow
        {
            get => MainWindowSlot;
            set => MainWindowSlot = value;
        }

        protected override bool HasStartupUri => StartupUri;
        protected override FakeWindow? CreateStartupWindow() => StartupWindowInstance;
        protected override FakeWindow? CreateMainWindow()
        {
            Events.Add("ShowMainWindow");
            return MainWindowInstance;
        }

        protected override void Show(FakeWindow window)
        {
            Shown.Add(window);
            MainWindowSlot ??= window;
        }

        protected override void Close(FakeWindow window) => Closed.Add(window);
        protected override void OnMainWindowShown() => Step("MainWindowShown");

        private void Step(string name)
        {
            Events.Add(name);
            if (name == FailAt)
                throw Failure!;
        }

        protected override void SuspendShutdown()
        {
            Events.Add("Suspend");
            base.SuspendShutdown();
        }

        protected override void ShowStartupWindow()
        {
            Events.Add("ShowStartupWindow");
            base.ShowStartupWindow();
        }

        protected override async Task InitializeContainerAsync()
        {
            Events.Add("Init");
#pragma warning disable VSTHRD003 // the gate is owned by the test
            await InitGate;
#pragma warning restore VSTHRD003
        }

        protected override void CloseStartupWindow()
        {
            Events.Add("CloseStartupWindow");
            base.CloseStartupWindow();
        }

        protected override void RestoreShutdown()
        {
            Events.Add("Restore");
            base.RestoreShutdown();
        }

        protected override void SetMainThread() => Step("SetMainThread");
        protected override void PublishServices() => Step("PublishServices");
        protected override void OnActivation() => Step("OnActivation");
        protected override void EnsureSingleInstance() => Step("EnsureSingleInstance");

        protected override void InitializeComponents()
        {
            Step("InitializeComponents");

            if (ModeSetByHook is { } mode)
                Mode = mode;
        }

        protected override void BeforeStartup() => Step("BeforeStartup");
        protected override void AfterServicesContainerBuild() => Step("AfterServicesContainerBuild");
        protected override void AfterStartup() => Step("AfterStartup");
        protected override void ExitIfInitializationHasFailed() => Step("ExitIfFailed");
        protected override void HandleException(Exception exception) => Handled.Add(exception);
        protected override void HandleEarlyException(Exception exception) => EarlyHandled.Add(exception);
        protected override void RequestExit(int exitCode) => ExitCodes.Add(exitCode);
    }
}
