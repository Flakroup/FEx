using FEx.Abstractions;
using FEx.Abstractions.Enums;
using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.Common.Extensions;
using FEx.DI.Abstractions;
using FEx.Extensions;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Controls;
using FEx.WPFx.Services;
using System;
using System.Threading.Tasks;

namespace FEx.WPFx;

public class Splash : FExInitialize, IFExPriorityInitialize
{
    private readonly IAppConfig _appConfig;
    private readonly IAsyncHelper _asyncHelper;
    private readonly IStatusService _statusService;

    public Task SplashTask { get; private set; }

    /// <inheritdoc />
    public int Priority { get; }

    public Splash(IAppConfig appConfig, IAsyncHelper asyncHelper, IStatusService statusService)
    {
        _appConfig = appConfig;
        _asyncHelper = asyncHelper;
        _statusService = statusService;
        Priority = -2;
    }

    public void WaitForSplashAndClose()
    {
        if (SplashTask?.IsFinished() == false)
            JoinableAsyncHelper.AwaitWithoutDeadlock(() => SplashTask);

        Close();
    }

    public void ShowSplash()
    {
        if (!_appConfig.SplashResourceName.IsNotNullOrEmptyString())
            return;

        TaskCompletionSource<bool> tcs = DispatcherService.ShowView(ShowSplashInternal, true);

        JoinableAsyncHelper.AwaitWithoutDeadlock(() => tcs.Task);
        JoinableAsyncHelper.AwaitWithoutDeadlock(() => SplashScreenWindow.InitializationTask);
    }

    public static void Close() => SplashScreenWindow.CloseIt?.Invoke(null, EventArgs.Empty);

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        base.OnInitialize();
        _statusService.GetOrAdd(markAsMain: true);
        SplashTask = _asyncHelper.FireAndForget(ShowSplash, AsyncMode.ThreadPool).Task;
    }

    private static SplashScreenWindow ShowSplashInternal() => FExServiceProvider.Get<SplashScreenWindow>();
}