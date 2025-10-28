using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Common.Abstractions.Interfaces;
using ReactiveUI;
using System.Reactive.Concurrency;

namespace FEx.MVVM.Rx;

public class FExMvvmRx : FExInitializable
{
    public static IStatusService StatusService { get; private set; }

    public FExMvvmRx(IStatusService statusService)
    {
        StatusService = statusService.Guard(nameof(statusService));
    }

    protected override void OnInitialize()
    {
#if NETFRAMEWORK
            RxApp.MainThreadScheduler = DispatcherScheduler.Current;
#else
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
#endif
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
    }
}