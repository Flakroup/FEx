using FEx.Abstractions.Interfaces;
using FEx.Fundamentals;
using FEx.Fundamentals.Helpers;
using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.MVVM.Abstractions;

public abstract class UIContextHandler : ViewModelBase, IUIContextAware
{
    protected Thread OriginThread { get; }

    protected IFExDispatcher Dispatcher => Foundation.Dispatcher;

    public UIContextHandler()
    {
        OriginThread = Thread.CurrentThread;
    }

    public void BeginInvokeOnMainThread(Action action) => Dispatcher.BeginInvokeOnMainThread(action);

    public bool CheckAccess() => Dispatcher.CheckAccess(GetSender());

    public void InvokeOnMainThread(Action action) => Dispatcher.InvokeOnMainThread(action, GetSender());

    public T InvokeOnMainThread<T>(Func<T> action) => Dispatcher.InvokeOnMainThread(action, GetSender());

    public void InvokeOnIdleMainThread(Action action) => Dispatcher.InvokeOnIdleMainThread(action, GetSender());

    public T InvokeOnIdleMainThread<T>(Func<T> action) => Dispatcher.InvokeOnIdleMainThread(action, GetSender());

    public async Task InvokeOnMainThreadAsync(Action action) =>
        await Dispatcher.InvokeOnMainThreadAsync(action, GetSender());

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<T> action) =>
        await Dispatcher.InvokeOnMainThreadAsync(action, GetSender());

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask) =>
        await Dispatcher.InvokeOnMainThreadAsync(funcTask, GetSender());

    public async Task InvokeOnMainThreadAsync(Func<Task> funcTask) =>
        await Dispatcher.InvokeOnMainThreadAsync(funcTask, GetSender());

    public void SendInThisOrMainThreadContext(Action action,
                                              SynchronizationContext synchronizationContext = null,
                                              uint timeout = 10000) =>
        Dispatcher.SendInThisOrMainThreadContext(action, synchronizationContext, timeout);

    public async Task InvokeOnIdleMainThreadAsync(Action action) =>
        await Dispatcher.InvokeOnIdleMainThreadAsync(action, GetSender());

    public async Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action) =>
        await Dispatcher.InvokeOnIdleMainThreadAsync(action, GetSender());

    protected abstract object GetDispatcherObject();

    protected virtual object GetSender() =>
        Dispatcher is DefaultDispatcher
            ? OriginThread
            : GetDispatcherObject();
}