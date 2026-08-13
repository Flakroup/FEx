using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.Common.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Controls;
using FEx.WPFx.Services;
using System;
using System.Threading.Tasks;

namespace FEx.WPFx;

public class Splash : FExInitializable, IFExPriorityInitialize
{
    private readonly IAppConfig _appConfig;
    private readonly IAsyncHelper _asyncHelper;
    private readonly IStatusService _statusService;

    public Task? SplashTask { get; private set; }

    /// <inheritdoc />
    public int Priority { get; }

    public Splash(IAppConfig appConfig, IAsyncHelper asyncHelper, IStatusService statusService)
    {
        _appConfig = appConfig;
        _asyncHelper = asyncHelper;
        _statusService = statusService;
        Priority = -2;
    }

    public static void Close() => SplashScreenWindow.CloseIt?.Invoke(null, EventArgs.Empty);

    public void WaitForSplashAndClose()
    {
        // VSTHRD003: AwaitWithoutDeadlock is the intended mechanism for safely observing this
        // externally-started splash task.
#pragma warning disable VSTHRD003
        if (SplashTask?.IsFinished() == false)
            JoinableAsyncHelper.AwaitWithoutDeadlock(() => SplashTask);
#pragma warning restore VSTHRD003

        Close();
    }

    public void ShowSplash()
    {
        if (!_appConfig.SplashResourceName.IsNotNullOrEmptyString())
            return;

        var tcs = DispatcherService.ShowView(ShowSplashInternal, true);

        // VSTHRD003: AwaitWithoutDeadlock safely observes these externally-started splash tasks.
#pragma warning disable VSTHRD003
        JoinableAsyncHelper.AwaitWithoutDeadlock(() => tcs.Task);
        JoinableAsyncHelper.AwaitWithoutDeadlock(() => SplashScreenWindow.InitializationTask);
#pragma warning restore VSTHRD003
    }

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        base.OnInitialize();
        _statusService.GetOrAdd(markAsMain: true);
        SplashTask = _asyncHelper.FireAndForget(ShowSplash, AsyncMode.ThreadPool).Task;
    }

    private static SplashScreenWindow ShowSplashInternal() => FExServiceProvider.Get<SplashScreenWindow>();
}