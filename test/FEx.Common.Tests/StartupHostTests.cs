using FEx.Common.Startup;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Common.Tests;

public class StartupHostTests
{
    private readonly FakeApp _app = new();

    private DesktopStartupHost<FakeWindow, FakeMode> Host => new(_app);

    [Fact]
    public async Task Container_init_is_awaited_not_blocked_on()
    {
        var gate = new TaskCompletionSource<bool>();
        _app.InitGate = gate.Task;

        var run = Host.RunAsync();

        // Control returns to the caller while the container is still building; no hook ran yet.
        run.IsCompleted.ShouldBeFalse();
        _app.Events.ShouldBe(["SetMainThread", "CreateStartupWindow", "Init"]);

        gate.SetResult(true);
        await run;

        _app.Events.ShouldContain("OnActivation");
    }

    [Fact]
    public async Task Steps_run_in_order_after_the_container_exists()
    {
        await Host.RunAsync();

        _app.Events.ShouldBe(
        [
            "SetMainThread", "CreateStartupWindow", "Init", "SetMainThread", "PublishServices", "OnActivation",
            "EnsureSingleInstance", "InitializeComponents", "BeforeStartup", "AfterServicesContainerBuild",
            "CreateMainWindow", "OnMainWindowShown", "AfterStartup", "ExitIfFailed"
        ]);
        _app.ExitCodes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Main_thread_is_set_before_the_startup_window_and_again_right_after_the_container()
    {
        await Host.RunAsync();

        var calls = _app.Events.FindAll(e => e == "SetMainThread").Count;
        calls.ShouldBe(2);
        _app.Events.IndexOf("SetMainThread").ShouldBeLessThan(_app.Events.IndexOf("CreateStartupWindow"));
        _app.Events.LastIndexOf("SetMainThread").ShouldBeGreaterThan(_app.Events.IndexOf("Init"));
        _app.Events.LastIndexOf("SetMainThread").ShouldBeLessThan(_app.Events.IndexOf("PublishServices"));
    }

    [Fact]
    public async Task Startup_window_can_use_main_thread_services_because_the_fallback_main_thread_is_set()
    {
        _app.StartupWindowNeedsMainThread = true;

        await Host.RunAsync();

        _app.ExitCodes.ShouldBeEmpty();
        _app.Shown.ShouldContain(_app.StartupWindowInstance);
    }

    [Fact]
    public async Task Main_window_is_not_created_while_init_is_pending_and_is_shown_after_it()
    {
        var gate = new TaskCompletionSource<bool>();
        _app.InitGate = gate.Task;

        var run = Host.RunAsync();
        _app.Shown.ShouldNotContain(_app.MainWindowInstance);
        _app.Events.ShouldNotContain("CreateMainWindow");

        gate.SetResult(true);
        await run;

        _app.Shown.ShouldContain(_app.MainWindowInstance);
        _app.MainWindow.ShouldBe(_app.MainWindowInstance);
    }

    [Fact]
    public async Task Startup_window_is_shown_before_init_and_closed_after()
    {
        var gate = new TaskCompletionSource<bool>();
        _app.InitGate = gate.Task;

        var run = Host.RunAsync();

        _app.Shown.ShouldBe([_app.StartupWindowInstance]);
        _app.Closed.ShouldBeEmpty();

        gate.SetResult(true);
        await run;

        _app.Closed.ShouldBe([_app.StartupWindowInstance]);
    }

    [Fact]
    public async Task Startup_window_does_not_keep_the_main_window_slot_when_the_app_shows_no_main_window()
    {
        _app.CreateNoMainWindow = true;

        await Host.RunAsync();

        _app.Shown.ShouldBe([_app.StartupWindowInstance]);
        _app.MainWindow.ShouldBeNull();
    }

    [Fact]
    public async Task Shutdown_is_explicit_while_init_runs_and_a_mode_set_by_a_hook_survives()
    {
        var gate = new TaskCompletionSource<bool>();
        _app.InitGate = gate.Task;
        _app.ModeSetByHook = FakeMode.OnMainWindowClose;

        var run = Host.RunAsync();
        _app.ShutdownMode.ShouldBe(FakeMode.Explicit);

        gate.SetResult(true);
        await run;

        _app.ShutdownMode.ShouldBe(FakeMode.OnMainWindowClose);
    }

    [Fact]
    public async Task Configured_shutdown_mode_is_restored_when_no_hook_changes_it()
    {
        _app.ShutdownMode = FakeMode.OnLastWindowClose;

        await Host.RunAsync();

        _app.ShutdownMode.ShouldBe(FakeMode.OnLastWindowClose);
    }

    [Fact]
    public async Task Failed_init_closes_the_startup_window_restores_shutdown_and_reaches_both_handlers_then_exits_non_zero()
    {
        var failure = new InvalidOperationException("boom");
        _app.InitGate = Task.FromException(failure);
        _app.ShutdownMode = FakeMode.OnLastWindowClose;

        await Host.RunAsync();

        // Early sink first, then the overridable handler, so crash reporting there still sees the failure.
        _app.Handlers.ShouldBe(["Early:boom", "Handle:boom"]);
        _app.ExitCodes.ShouldBe([1]);
        _app.Closed.ShouldBe([_app.StartupWindowInstance]);
        _app.ShutdownMode.ShouldBe(FakeMode.OnLastWindowClose);
        _app.Events.ShouldNotContain("PublishServices");
    }

    [Fact]
    public async Task Startup_uri_is_rejected_before_any_window_or_container()
    {
        _app.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);

        await Host.RunAsync();

        _app.Handlers.ShouldBe(["Early:" + _app.LastMessage, "Handle:" + _app.LastMessage]);
        _app.LastMessage.ShouldContain("StartupUri");
        _app.ExitCodes.ShouldBe([1]);
        _app.Events.ShouldBeEmpty();
        _app.Shown.ShouldBeEmpty();
    }

    [Fact]
    public async Task Hook_failure_after_the_services_are_published_skips_the_early_sink_and_exits_non_zero()
    {
        _app.FailAt = "BeforeStartup";

        await Host.RunAsync();

        _app.Handlers.ShouldBe(["Handle:boom"]);
        _app.ExitCodes.ShouldBe([1]);
        _app.Events.ShouldNotContain("CreateMainWindow");
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
    private sealed class FakeApp : IDesktopApp<FakeWindow, FakeMode>
    {
        private bool _mainThreadSet;

        public List<string> Events { get; } = [];
        public List<string> Handlers { get; } = [];
        public List<int> ExitCodes { get; } = [];
        public List<FakeWindow> Shown { get; } = [];
        public List<FakeWindow> Closed { get; } = [];
        public FakeWindow StartupWindowInstance { get; } = new("startup");
        public FakeWindow MainWindowInstance { get; } = new("main");
        public FakeMode? ModeSetByHook { get; set; }
        public bool CreateNoMainWindow { get; set; }
        public bool StartupWindowNeedsMainThread { get; set; }
        public Task InitGate { get; set; } = Task.CompletedTask;
        public string? FailAt { get; set; }
        public string LastMessage { get; private set; } = "";

        public Uri? StartupUri { get; set; }
        public FakeMode ShutdownMode { get; set; } = FakeMode.OnLastWindowClose;
        public FakeMode ExplicitShutdownMode => FakeMode.Explicit;
        public FakeWindow? MainWindow { get; set; }

        public FakeWindow? CreateStartupWindow()
        {
            Events.Add("CreateStartupWindow");

            // Like an FExWindow touching the static dispatcher, which throws while no main thread is set.
            if (StartupWindowNeedsMainThread && !_mainThreadSet)
                throw new ArgumentNullException("mainThread");

            return StartupWindowInstance;
        }

        public FakeWindow? CreateMainWindow()
        {
            Events.Add("CreateMainWindow");
            return CreateNoMainWindow ? null : MainWindowInstance;
        }

        public void Show(FakeWindow window)
        {
            Shown.Add(window);
            MainWindow ??= window;
        }

        public void Close(FakeWindow window) => Closed.Add(window);

        public void OnMainWindowShown() => Step("OnMainWindowShown");

        public async Task InitializeContainerAsync()
        {
            Events.Add("Init");
#pragma warning disable VSTHRD003 // the gate is owned by the test
            await InitGate;
#pragma warning restore VSTHRD003
        }

        public void SetMainThread()
        {
            Events.Add("SetMainThread");
            _mainThreadSet = true;
        }

        public void PublishServices() => Step("PublishServices");
        public void OnActivation() => Step("OnActivation");
        public void EnsureSingleInstance() => Step("EnsureSingleInstance");

        public void InitializeComponents()
        {
            Step("InitializeComponents");

            if (ModeSetByHook is { } mode)
                ShutdownMode = mode;
        }

        public void BeforeStartup() => Step("BeforeStartup");
        public void AfterServicesContainerBuild() => Step("AfterServicesContainerBuild");
        public void AfterStartup() => Step("AfterStartup");
        public void ExitIfInitializationHasFailed() => Step("ExitIfFailed");

        public void HandleEarlyException(Exception exception)
        {
            LastMessage = exception.Message;
            Handlers.Add("Early:" + exception.Message);
        }

        public void HandleException(Exception exception)
        {
            LastMessage = exception.Message;
            Handlers.Add("Handle:" + exception.Message);
        }

        public void RequestExit(int exitCode) => ExitCodes.Add(exitCode);

        private void Step(string name)
        {
            Events.Add(name);
            if (name == FailAt)
                throw new InvalidOperationException("boom");
        }
    }
}
