using Avalonia.Threading;
using FEx.Abstractions;
using FEx.Fundamentals;
using FEx.MVVM.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

public class AvaloniaDispatcher : IFExDispatcher, IUIContextExecutor
{
    protected Dispatcher Dispatcher => Dispatcher.UIThread;

    public void BeginInvokeOnMainThread(Action action)
    {
        Foundation.AsyncHelper.FireAndForget(() => Dispatcher.Invoke(action), asyncMode: AsyncMode.ThreadPool);
    }

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func) => await Dispatcher.InvokeAsync(func);

    public async Task InvokeOnMainThreadAsync(Action action) => await Dispatcher.InvokeAsync(action);

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask) => await Dispatcher.InvokeAsync(funcTask);

    public async Task InvokeOnMainThreadAsync(Func<Task> funcTask) => await Dispatcher.InvokeAsync(funcTask);

    public void SendInThisOrMainThreadContext(Action action,
                                              SynchronizationContext synchronizationContext = null,
                                              uint timeout = 10000)
    {
        Dispatcher.Invoke(action);
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
}