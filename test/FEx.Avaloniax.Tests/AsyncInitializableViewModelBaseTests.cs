using FEx.Avaloniax.Abstractions;
using FEx.Avaloniax.Abstractions.Interfaces;
using NSubstitute;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Avaloniax.Tests;

public sealed class AsyncInitializableViewModelBaseTests
{
    [Fact]
    public async Task ResetAsync_DuringInitialization_WaitsForIt()
    {
        using var sut = new GatedViewModel();

        var initialization = sut.InitializeAsync();
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        await sut.HookEntered.Task;
#pragma warning restore VSTHRD003

        // The hook holds the initialization lock, so the reset cannot clear the state under it.
        var reset = sut.ResetAsync();
        reset.IsCompleted.ShouldBeFalse();

        sut.HookGate.SetResult(true);
        await initialization;
        await reset;

        sut.IsInitialized.ShouldBeFalse();
        sut.CurrentInitializationTask.ShouldBeNull();
    }

    private sealed class GatedViewModel : AsyncInitializableViewModelBase
    {
        public TaskCompletionSource<bool> HookEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> HookGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task? CurrentInitializationTask => _initializationTask;

        public GatedViewModel()
            : base(Substitute.For<INavigationService>())
        {
        }

        protected override async Task OnInitializeAsync()
        {
            HookEntered.TrySetResult(true);
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
            await HookGate.Task;
#pragma warning restore VSTHRD003
        }
    }
}
