using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Legacy.Asyncx.Abstractions.Interfaces;
using FEx.Legacy.Mvvm.ViewModels;
using NSubstitute;
using Shouldly;
using StrongInject;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Legacy.Tests;

[Collection("FExServiceProvider")] // Static state
public sealed partial class ThreadingAwareViewModelResetTests : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        FExServiceProvider.Release();
#pragma warning disable IDISP001 // The static provider owns the container until Release
        await FExServiceProvider.InitializeAsync<SubstituteContainer>();
#pragma warning restore IDISP001
    }

    public ValueTask DisposeAsync()
    {
        FExServiceProvider.Release();

        return ValueTask.CompletedTask;
    }

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

    private sealed class GatedViewModel : ThreadingAwareViewModel
    {
        public TaskCompletionSource<bool> HookEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> HookGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task? CurrentInitializationTask => _initializationTask;

        protected override async Task OnInitializeAsync()
        {
            HookEntered.TrySetResult(true);
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
            await HookGate.Task;
#pragma warning restore VSTHRD003
        }
    }

    /// <summary>Just enough of a container for the view model's constructor to resolve its tasks handler.</summary>
    public sealed partial class SubstituteContainer : IContainer<IFExStrongInjectServiceProvider>,
        IContainer<IFExServiceContainer>
    {
        private readonly IFExStrongInjectServiceProvider _provider = Substitute.For<IFExStrongInjectServiceProvider>();
        private readonly IFExServiceContainer _services = Substitute.For<IFExServiceContainer>();

        public SubstituteContainer()
        {
            _services.ResolveService<ITasksHandler>().Returns(Substitute.For<ITasksHandler>());
            _services.ResolveServicesAsync<IFExServiceProvider>()
                .Returns(Task.FromResult<IEnumerable<IFExServiceProvider>>([]));
        }

        [Factory]
        private IFExStrongInjectServiceProvider GetProvider() => _provider;

        [Factory]
        private IFExServiceContainer GetServices() => _services;
    }
}
