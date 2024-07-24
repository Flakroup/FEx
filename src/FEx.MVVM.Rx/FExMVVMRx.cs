using FEx.DependencyInjection.Abstractions;
using ReactiveUI;
using System.Reactive.Concurrency;

namespace FEx.MVVM.Rx;

public class FExMVVMRx : InitializeOnlyModule
{
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