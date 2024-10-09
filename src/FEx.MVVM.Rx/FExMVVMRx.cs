using FEx.Abstractions.Interfaces;
using FEx.Common.Extensions;
using FEx.DI.Abstractions;
using FEx.MVVM.Rx.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.Reactive.Concurrency;

namespace FEx.MVVM.Rx;

public class FExMvvmRx : InitializeModule<IFExMvvmRxContainer>
{
    public static IStatusService StatusService { get; private set; }

    public FExMvvmRx(IStatusService statusService)
    {
        StatusService = statusService.Guard(nameof(statusService));
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();

#if NETFRAMEWORK
            RxApp.MainThreadScheduler = DispatcherScheduler.Current;
#else
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
#endif
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
    }

    /// <inheritdoc />
    protected override void AddServices(IFExMvvmRxContainer container, IServiceCollection services) =>
        FExMvvmRxModule.AddServices(container, services);
}