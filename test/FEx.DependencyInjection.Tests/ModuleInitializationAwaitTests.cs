using FEx.Agnostics.TestMocks;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using StrongInject;
using StrongInject.Modules;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FEx.DependencyInjection.Tests;

/// <summary>
/// Pins that the container build awaits every module's async initialization. A module that really yields (database or
/// file I/O) used to be fire-and-forget: the build completed while it was still running.
/// </summary>
[Collection("FExServiceProvider")] // shares the static FExServiceProvider state with the other tests
public sealed class ModuleInitializationAwaitTests : IDisposable
{
    public void Dispose()
    {
        FExServiceProvider.Release();
        GatedModule.Reset();
    }

    [Fact]
    public async Task Container_build_does_not_complete_before_a_module_finishes_its_async_initialization()
    {
        FExServiceProvider.Release();
        GatedModule.Reset();

        var build = FExServiceProvider.InitializeAsync<GatedContainer>().AsTask();
        await GatedModule.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // The module is running and blocked on its gate: the build must still be pending.
        await Task.Delay(200, TestContext.Current.CancellationToken);
        build.IsCompleted.ShouldBeFalse();
        GatedModule.Finished.ShouldBeFalse();

        GatedModule.Gate.SetResult(true);
        await build.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        GatedModule.Finished.ShouldBeTrue();
    }

    [Fact]
    public async Task A_failed_initialization_leaves_no_half_built_container_and_a_retry_runs_the_module_again()
    {
        FExServiceProvider.Release();
        ThrowOnceModule.Calls = 0;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            using var failed = await FExServiceProvider.InitializeAsync<ThrowOnceContainer>();
        });

        // The retry must not be served the container of the failed attempt through the idempotency check.
        using var retried = await FExServiceProvider.InitializeAsync<ThrowOnceContainer>();

        ThrowOnceModule.Calls.ShouldBe(2);
    }
}

public sealed class ThrowOnceModule : InitializeOnlyModule
{
    public static int Calls { get; set; }

    public override ValueTask OnCompleteInitializationAsync(IServiceCollection services) =>
        ++Calls == 1
            ? throw new InvalidOperationException("db down")
            : ValueTask.CompletedTask;
}

public sealed class GatedModule : InitializeOnlyModule
{
    public static TaskCompletionSource<bool> Entered { get; private set; } = new();
    public static TaskCompletionSource<bool> Gate { get; private set; } = new();
    public static bool Finished { get; private set; }

    public static void Reset()
    {
        Entered = new();
        Gate = new();
        Finished = false;
    }

    public override async ValueTask OnCompleteInitializationAsync(IServiceCollection services)
    {
        Entered.TrySetResult(true);
#pragma warning disable VSTHRD003 // the gate is owned by the test
        await Gate.Task;
#pragma warning restore VSTHRD003
        Finished = true;
    }
}

#pragma warning disable SI1105
[RegisterModule(typeof(CollectionsModule))]
[RegisterModule(typeof(FExDependencyInjectionModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(GatedModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public sealed partial class GatedContainer : TestBase, IFExDependencyInjectionContainer,
    IContainer<IInitializeModule<IServiceCollection>[]>
{
    [Factory]
    public static ILogger CreateLogger() => NullLogger.Instance;
}

[RegisterModule(typeof(CollectionsModule))]
[RegisterModule(typeof(FExDependencyInjectionModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(ThrowOnceModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public sealed partial class ThrowOnceContainer : TestBase, IFExDependencyInjectionContainer,
    IContainer<IInitializeModule<IServiceCollection>[]>
{
    [Factory]
    public static ILogger CreateLogger() => NullLogger.Instance;
}
#pragma warning restore SI1105
