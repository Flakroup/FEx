using FEx.Abstractions;
using FEx.Fundamentals.Abstractions;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FEx.WPFx;

public class DispatcherContextExecutor : FExDispatcher, IUIContextExecutor, IFExDispatcher
{
    public DispatcherContextExecutor(ILogger<DispatcherContextExecutor> logger)
        : base(logger)
    {
    }

    public override void BeginInvokeOnMainThread(Action action)
    {
        DispatcherService.BeginInvoke(action);
    }

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func) => await ExecuteActionInUIContextAsync(func);

    public override async Task InvokeOnMainThreadAsync(Action action) => await ExecuteActionInUIContextAsync(action);

    public override Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask) =>
        throw new NotImplementedException();

    public override Task InvokeOnMainThreadAsync(Func<Task> funcTask) =>
        throw new NotImplementedException();

    public void ExecuteActionInIdleUIContext(Action action, object sender = null)
    {
        DispatcherService.ExecuteActionInDispatcherContext(action, (DispatcherObject)sender,
            DispatcherPriority.ApplicationIdle);
    }

    public T ExecuteActionInIdleUIContext<T>(Func<T> action, object sender = null) =>
        DispatcherService.ExecuteActionInDispatcherContext(action, (DispatcherObject)sender,
            DispatcherPriority.ApplicationIdle);

    public void ExecuteActionInUIContext(Action action, object sender = null)
    {
        DispatcherService.ExecuteActionInDispatcherContext(action, (DispatcherObject)sender);
    }

    public async Task<T> ExecuteActionInIdleUIContextAsync<T>(Func<T> action, object sender = null) =>
        await DispatcherService.ExecuteActionInDispatcherContextAsync(action, (DispatcherObject)sender,
            DispatcherPriority.ApplicationIdle);

    public async Task ExecuteActionInUIContextAsync(Action action, object sender = null)
    {
        await DispatcherService.ExecuteActionInDispatcherContextAsync(action, (DispatcherObject)sender);
    }

    public T ExecuteActionInUIContext<T>(Func<T> action, object sender = null) =>
        DispatcherService.ExecuteActionInDispatcherContext(action, (DispatcherObject)sender);

    public async Task ExecuteActionInIdleUIContextAsync(Action action, object sender = null)
    {
        await DispatcherService.ExecuteActionInDispatcherContextAsync(action, (DispatcherObject)sender,
            DispatcherPriority.ApplicationIdle);
    }

    public async Task<T> ExecuteActionInUIContextAsync<T>(Func<T> action, object sender = null) =>
        await DispatcherService.ExecuteActionInDispatcherContextAsync(action, (DispatcherObject)sender);

    public bool CheckAccess(object sender = null) => DispatcherService.CheckAccess((DispatcherObject)sender);
}