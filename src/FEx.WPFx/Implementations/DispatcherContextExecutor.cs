using FEx.Basics.Abstractions;
using FEx.WPFx.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FEx.WPFx.Implementations;

public class DispatcherContextExecutor : FExDispatcher
{
    public DispatcherContextExecutor(ILogger<DispatcherContextExecutor> logger)
        : base(logger)
    {
    }

    public override bool CheckAccess(object sender = null) => DispatcherService.CheckAccess((DispatcherObject)sender);

    public override void BeginInvokeOnMainThread(Action action) => DispatcherService.BeginInvoke(action);

    public override void InvokeOnIdleMainThread(Action action, object sender = null) =>
        DispatcherService.ExecuteActionInDispatcherContext(action,
            (DispatcherObject)sender,
            DispatcherPriority.ApplicationIdle);

    public override T InvokeOnIdleMainThread<T>(Func<T> action, object sender = null) =>
        DispatcherService.ExecuteActionInDispatcherContext(action,
            (DispatcherObject)sender,
            DispatcherPriority.ApplicationIdle);

    public override void InvokeOnMainThread(Action action, object sender = null) =>
        DispatcherService.ExecuteActionInDispatcherContext(action, (DispatcherObject)sender);

    public override async Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object sender = null) =>
        await DispatcherService.ExecuteActionInDispatcherContextAsync(action,
            (DispatcherObject)sender,
            DispatcherPriority.ApplicationIdle);

    public override async Task InvokeOnMainThreadAsync(Action action, object sender = null) =>
        await DispatcherService.ExecuteActionInDispatcherContextAsync(action, (DispatcherObject)sender);

    public override T InvokeOnMainThread<T>(Func<T> action, object sender = null) =>
        DispatcherService.ExecuteActionInDispatcherContext(action, (DispatcherObject)sender);

    public override async Task InvokeOnIdleMainThreadAsync(Action action, object sender = null) =>
        await DispatcherService.ExecuteActionInDispatcherContextAsync(action,
            (DispatcherObject)sender,
            DispatcherPriority.ApplicationIdle);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object sender = null) =>
        await DispatcherService.ExecuteActionInDispatcherContextAsync(action, (DispatcherObject)sender);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender = null) =>
        await DispatcherService.ExecuteTaskInDispatcherContextAsync(funcTask);

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender = null) =>
        await DispatcherService.ExecuteTaskInDispatcherContextAsync(funcTask);
}