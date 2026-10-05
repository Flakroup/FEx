using FEx.Common.Abstractions.Interfaces;
using FEx.MVVM.Rx;
using FEx.MVVM.Rx.Utilities;
using ReactiveUI;
using Shouldly;
using System.Reactive.Concurrency;
using Xunit;

namespace FEx.MVVM.Tests;

public class FExMvvmRxTests
{
    [Fact]
    public void Initialize_configures_the_reactive_schedulers_and_is_idempotent()
    {
        IStatusService service = new StatusService();
        var module = new FExMvvmRx(service);
        RxSchedulers.TaskpoolScheduler = ImmediateScheduler.Instance;
        RxSchedulers.MainThreadScheduler = DefaultScheduler.Instance;

        module.Initialize();
        module.Initialize();

        module.IsInitialized.ShouldBeTrue();
        RxSchedulers.TaskpoolScheduler.ShouldBeSameAs(TaskPoolScheduler.Default);
        RxSchedulers.MainThreadScheduler.ShouldBeSameAs(CurrentThreadScheduler.Instance);
    }

    [Fact]
    public void Constructor_exposes_the_status_service()
    {
        IStatusService service = new StatusService();

        _ = new FExMvvmRx(service);

        FExMvvmRx.StatusService.ShouldBeSameAs(service);
    }
}
