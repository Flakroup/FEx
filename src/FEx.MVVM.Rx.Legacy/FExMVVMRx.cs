using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Common.Abstractions.Interfaces;
using ReactiveUI;
using ReactiveUI.Builder;
using System.Reactive.Concurrency;

namespace FEx.MVVM.Rx.Legacy;

public class FExMvvmRx : FExInitializable
{
    // Assigned in the constructor from the DI-provided service (guarded non-null); this instance is created during module init before any consumer reads it.
    public static IStatusService StatusService { get; private set; } = null!;

    public FExMvvmRx(IStatusService statusService)
    {
        StatusService = statusService.Guard(nameof(statusService));
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();

        // ReactiveUI 23.x dropped automatic initialization: the first WhenAny/WhenAnyValue triggers
        // ReactiveNotifyPropertyChangedMixin's static ctor, which throws unless RxApp was built first. Build the
        // platform-agnostic core here - enough for WhenAnyValue across every host (WPF, headless console, Avalonia).
        // Platform services (WPF bindings/activation, Avalonia UseReactiveUI) are layered by the platform projects,
        // not by this cross-platform core module.
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();

#if NETFRAMEWORK
            RxSchedulers.MainThreadScheduler = DispatcherScheduler.Current;
#else
        if (RxSchedulers.MainThreadScheduler is DefaultScheduler)
            RxSchedulers.MainThreadScheduler = CurrentThreadScheduler.Instance;
#endif
        RxSchedulers.TaskpoolScheduler = TaskPoolScheduler.Default;
    }
}