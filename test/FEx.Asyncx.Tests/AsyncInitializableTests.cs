using FEx.Asyncx.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using Shouldly;
using StrongInject;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Asyncx.Tests;

public sealed class AsyncInitializableTests
{
    // Bounds every gated wait, so a regression fails the test instead of hanging the run.
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task HookWithoutBaseCall_StillInitializesDependencies_AndCompletes()
    {
        using var dependency = new RecordingInitializable("dependency", new());
        using var sut = new HookOnlyInitializable([dependency]);

        await sut.InitializeAsync();

        dependency.IsInitialized.ShouldBeTrue();
        sut.DependencyWasInitializedInHook.ShouldBeTrue();
        sut.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public async Task Steps_RunInOrder_PreDependencyHook_Dependencies_ThenHook()
    {
        var order = new ConcurrentQueue<string>();
        using var dependency = new RecordingInitializable("dependency", order);
        using var sut = new RecordingInitializable("sut", order, [dependency]);

        await sut.InitializeAsync();

        order.ShouldBe(["sut:before", "dependency:before", "dependency", "sut"]);
        sut.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public async Task Dependencies_AreAllInitializedBeforeTheHook()
    {
        var order = new ConcurrentQueue<string>();
        using var first = new RecordingInitializable("first", order);
        using var second = new RecordingInitializable("second", order);
        using var sut = new RecordingInitializable("sut", order, [first, second]);

        await sut.InitializeAsync();

        order.Count.ShouldBe(6);
        order.ToArray()[5].ShouldBe("sut");
    }

    /// <summary>
    /// The initializing caller resumes only through a queue the test drains, so the reset clears the state before
    /// the caller sees it: the reset waits for the in-flight run, and InitializeAsync still completes initialized.
    /// </summary>
    [Fact]
    public async Task ResetAsync_DuringInitialization_WaitsForIt_AndInitializeAsyncStillCompletesInitialized()
    {
        using var sut = new GatedInitializable();
        using var callerContext = new QueueSynchronizationContext();

        var initialization = callerContext.Start(sut.InitializeAsync);
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        await sut.HookEntered.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003

        // The hook holds the initialization lock, so the reset cannot clear the state under it.
        var reset = sut.ResetAsync();
        reset.IsCompleted.ShouldBeFalse();

        sut.HookGate.SetResult(true);
        await reset.WaitAsync(Bound, TestContext.Current.CancellationToken);
        sut.IsInitialized.ShouldBeFalse();
        sut.CurrentInitializationTask.ShouldBeNull();

        // The caller now finds its run reset and initializes again.
        callerContext.RunUntilCompleted(initialization);
        await initialization.WaitAsync(Bound, TestContext.Current.CancellationToken);

        sut.IsInitialized.ShouldBeTrue();
        (sut.CurrentInitializationTask is not null).ShouldBeTrue();
        sut.HookRuns.ShouldBe(2);
    }

    [Fact]
    public async Task ResetAsync_AfterInitialization_AllowsInitializingAgain()
    {
        using var sut = new GatedInitializable();
        sut.HookGate.SetResult(true);
        await sut.InitializeAsync();

        await sut.ResetAsync();
        sut.IsInitialized.ShouldBeFalse();
        sut.CurrentInitializationTask.ShouldBeNull();

        await sut.InitializeAsync();

        sut.IsInitialized.ShouldBeTrue();
        (sut.CurrentInitializationTask is not null).ShouldBeTrue();
        sut.HookRuns.ShouldBe(2);
    }

    /// <summary>
    /// A reset that clears the published task before its run takes the lock must not turn InitializeAsync into a
    /// successful no-op.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_ResetBetweenPublishingAndRunning_StillCompletesInitialized()
    {
        using var sut = new GatedInitializable { GateFirstReset = true };
        sut.HookGate.SetResult(true);

        // The first reset holds the initialization lock in OnResetAsync.
        var firstReset = sut.ResetAsync();
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        await sut.ResetEntered.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003

        // Queued on the lock first, so it runs after the first reset and before the initialization run.
        var secondReset = sut.ResetAsync();
        secondReset.IsCompleted.ShouldBeFalse();

        // Publishes a task whose run queues on the lock behind the second reset, which then clears it.
        var initialization = sut.InitializeAsync();

        sut.ResetGate.SetResult(true);
        await firstReset.WaitAsync(Bound, TestContext.Current.CancellationToken);
        await secondReset.WaitAsync(Bound, TestContext.Current.CancellationToken);
        await initialization.WaitAsync(Bound, TestContext.Current.CancellationToken);

        sut.IsInitialized.ShouldBeTrue();
        (sut.CurrentInitializationTask is not null).ShouldBeTrue();
        sut.HookRuns.ShouldBe(1);
    }

    [Fact]
    public async Task InitializeAsync_AfterFailure_RethrowsUntilReset()
    {
        using var sut = new GatedInitializable { FailNextRun = true };
        sut.HookGate.SetResult(true);

        await Should.ThrowAsync<InvalidOperationException>(sut.InitializeAsync);
        await Should.ThrowAsync<InvalidOperationException>(sut.InitializeAsync);
        sut.IsInitialized.ShouldBeFalse();
        sut.HookRuns.ShouldBe(1);

        await sut.ResetAsync();
        await sut.InitializeAsync();

        sut.IsInitialized.ShouldBeTrue();
        sut.HookRuns.ShouldBe(2);
    }

    /// <summary>Queues continuations until <see cref="RunUntilCompleted" /> drains them on the calling thread.</summary>
    private sealed class QueueSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        public Task Start(Func<Task> action)
        {
            var previous = Current;
            SetSynchronizationContext(this);

            try
            {
                return action();
            }
            finally
            {
                SetSynchronizationContext(previous);
            }
        }

        public void RunUntilCompleted(Task task)
        {
            var previous = Current;
            SetSynchronizationContext(this);

            try
            {
                while (!task.IsCompleted)
                {
                    // The timeout only bounds a broken run; ordering never depends on it.
                    if (!_queue.TryTake(out var item, TimeSpan.FromSeconds(30)))
                        throw new TimeoutException("The caller never resumed");

                    item.Callback(item.State);
                }
            }
            finally
            {
                SetSynchronizationContext(previous);
            }
        }

        public void Dispose() => _queue.Dispose();
    }

    [Fact]
    public async Task AsyncAutoInitializable_Initialize_StartsInitialization()
    {
        using var sut = new AutoInitializable();

        ((IRequiresInitialization)sut).Initialize();

#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        await sut.HookRan.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003
    }

    private sealed class AutoInitializable : AsyncAutoInitializable
    {
        public TaskCompletionSource<bool> HookRan { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public AutoInitializable()
            : base([])
        {
        }

        protected override Task OnInitializeAsync()
        {
            HookRan.TrySetResult(true);

            return Task.CompletedTask;
        }
    }

    private sealed class HookOnlyInitializable : AsyncInitializable
    {
        private readonly IAsyncInitializable _dependency;

        public bool DependencyWasInitializedInHook { get; private set; }

        public HookOnlyInitializable(IAsyncInitializable[] dependencies)
            : base(dependencies) =>
            _dependency = dependencies[0];

        // Deliberately no base call: forgetting it must be harmless.
        protected override Task OnInitializeAsync()
        {
            DependencyWasInitializedInHook = _dependency.IsInitialized;

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingInitializable : AsyncInitializable
    {
        private readonly string _name;
        private readonly ConcurrentQueue<string> _order;

        public RecordingInitializable(string name, ConcurrentQueue<string> order)
            : this(name, order, [])
        {
        }

        public RecordingInitializable(string name, ConcurrentQueue<string> order, IAsyncInitializable[] dependencies)
            : base(dependencies)
        {
            _name = name;
            _order = order;
            // Dependencies are keyed by type name; distinct names keep two instances of this class apart.
            TypeFullName = $"{TypeFullName}:{name}";
        }

        protected override Task OnBeforeDependenciesInitializationAsync()
        {
            _order.Enqueue($"{_name}:before");

            return Task.CompletedTask;
        }

        protected override Task OnInitializeAsync()
        {
            _order.Enqueue(_name);

            return Task.CompletedTask;
        }
    }

    private sealed class GatedInitializable : AsyncInitializable
    {
        private int _resets;

        public TaskCompletionSource<bool> HookEntered { get; } = NewSignal();
        public TaskCompletionSource<bool> HookGate { get; } = NewSignal();
        public TaskCompletionSource<bool> ResetEntered { get; } = NewSignal();
        public TaskCompletionSource<bool> ResetGate { get; } = NewSignal();

        public bool FailNextRun { get; set; }
        public bool GateFirstReset { get; set; }
        public int HookRuns { get; private set; }

        public Task? CurrentInitializationTask => _initializationTask;

        public GatedInitializable()
            : base([])
        {
        }

        protected override async Task OnInitializeAsync()
        {
            HookRuns++;
            HookEntered.TrySetResult(true);
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
            await HookGate.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003

            if (!FailNextRun)
                return;

            FailNextRun = false;

            throw new InvalidOperationException("boom");
        }

        protected override async Task OnResetAsync()
        {
            if (!GateFirstReset || ++_resets > 1)
                return;

            ResetEntered.SetResult(true);
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
            await ResetGate.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003
        }

        private static TaskCompletionSource<bool> NewSignal() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
