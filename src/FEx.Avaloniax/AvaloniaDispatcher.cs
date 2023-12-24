using Avalonia.Threading;
using FEx.Fundamentals;
using FEx.MVVM.Abstractions;
using FEx.Utilities.Implementations;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

public class AvaloniaDispatcher : FExDispatcher, IUIContextExecutor
{
    protected static Dispatcher Dispatcher => Dispatcher.UIThread;

    public AvaloniaDispatcher(ILogger logger)
        : base(logger)
    {
    }

    public bool CheckAccess(object sender = null) => Dispatcher.CheckAccess();

    public void ExecuteActionInIdleUIContext(Action action, object sender = null)
    {
        Dispatcher.Invoke(action, DispatcherPriority.ApplicationIdle);
    }

    public T ExecuteActionInIdleUIContext<T>(Func<T> action, object sender = null) =>
        Dispatcher.Invoke(action, DispatcherPriority.ApplicationIdle);

    public void ExecuteActionInUIContext(Action action, object sender = null)
    {
        Dispatcher.Invoke(action);
    }

    public T ExecuteActionInUIContext<T>(Func<T> action, object sender = null) => Dispatcher.Invoke(action);

    public async Task ExecuteActionInIdleUIContextAsync(Action action, object sender = null)
    {
        await Dispatcher.InvokeAsync(action, DispatcherPriority.ApplicationIdle);
    }

    public async Task<T> ExecuteActionInIdleUIContextAsync<T>(Func<T> action, object sender = null) =>
        await Dispatcher.InvokeAsync(action, DispatcherPriority.ApplicationIdle);

    public async Task ExecuteActionInUIContextAsync(Action action, object sender = null)
    {
        await Dispatcher.InvokeAsync(action);
    }

    public async Task<T> ExecuteActionInUIContextAsync<T>(Func<T> action, object sender = null) =>
        await Dispatcher.InvokeAsync(action);

    public override void BeginInvokeOnMainThread(Action action)
    {
        Foundation.AsyncHelper.FireAndForget(() => Dispatcher.Invoke(action), AsyncMode.ThreadPool);
    }

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func) => await Dispatcher.InvokeAsync(func);

    public override async Task InvokeOnMainThreadAsync(Action action) => await Dispatcher.InvokeAsync(action);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask) =>
        await Dispatcher.InvokeAsync(funcTask);

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask) => await Dispatcher.InvokeAsync(funcTask);

    public override void SendInThisOrMainThreadContext(Action action,
                                                       SynchronizationContext synchronizationContext = null,
                                                       uint timeout = 10000)
    {
        Dispatcher.Invoke(action);
    }
}