using FEx.Asyncx.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using Shouldly;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Asyncx.Tests;

public sealed class AsyncInitializableTests
{
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
    public async Task Dependencies_AreInitializedBeforeTheHook()
    {
        var order = new ConcurrentQueue<string>();
        using var first = new RecordingInitializable("first", order);
        using var second = new RecordingInitializable("second", order);
        using var sut = new RecordingInitializable("sut", order, [first, second]);

        await sut.InitializeAsync();

        order.Count.ShouldBe(3);
        order.ToArray()[2].ShouldBe("sut");
        sut.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public async Task Reset_DuringInitialization_WaitsForIt_AndLeavesAConsistentState()
    {
        using var sut = new GatedInitializable();

        var initialization = sut.InitializeAsync();
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        await sut.HookEntered.Task;
#pragma warning restore VSTHRD003

        // The hook holds the initialization lock, so the reset cannot clear the state under it.
        var reset = sut.ResetAsync();
        reset.IsCompleted.ShouldBeFalse();

        sut.Gate.SetResult(true);
        await initialization;
        await reset;

        sut.IsInitialized.ShouldBeFalse();
        sut.CurrentInitializationTask.ShouldBeNull();
    }

    [Fact]
    public async Task Reset_AfterInitialization_AllowsInitializingAgain()
    {
        using var sut = new GatedInitializable();
        sut.Gate.SetResult(true);
        await sut.InitializeAsync();

        await sut.ResetAsync();
        sut.IsInitialized.ShouldBeFalse();
        sut.CurrentInitializationTask.ShouldBeNull();

        await sut.InitializeAsync();

        sut.IsInitialized.ShouldBeTrue();
        (sut.CurrentInitializationTask is not null).ShouldBeTrue();
        sut.HookRuns.ShouldBe(2);
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

        protected override Task OnInitializeAsync()
        {
            _order.Enqueue(_name);

            return Task.CompletedTask;
        }
    }

    private sealed class GatedInitializable : AsyncInitializable
    {
        public TaskCompletionSource<bool> HookEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
            await Gate.Task;
#pragma warning restore VSTHRD003
        }
    }
}
