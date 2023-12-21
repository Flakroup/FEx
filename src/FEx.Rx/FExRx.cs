using ReactiveUI;
using System.Reactive.Concurrency;

namespace FEx.Rx;

public static class FExRx
{
    public static void Init()
    {
#if NETFRAMEWORK
            RxApp.MainThreadScheduler = DispatcherScheduler.Current;
#else
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
#endif
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
    }
}