using FEx.Abstractions;
using FEx.Asyncx;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Maui;

public class FExMauiDispatcher : BindableObject, IFExDispatcher
{
    public SynchronizationContext MainThreadSynchronizationContext { get; private set; }

    public bool IsDeadlockMonitoringEnabled { get; private set; }

    public FExMauiDispatcher()
    {
        if (MainThread.IsMainThread)
            MainThreadSynchronizationContext = SynchronizationContext.Current;
        else
            FExAsyncx.AsyncHelper.FireTaskAndForget(async () => MainThreadSynchronizationContext = await MainThread.GetMainThreadSynchronizationContextAsync());
    }

    public void BeginInvokeOnMainThread(Action action)
    {
        Dispatcher.Dispatch(action);
    }

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func) => await Dispatcher.DispatchAsync(func);

    public async Task InvokeOnMainThreadAsync(Action action)
    {
        await Dispatcher.DispatchAsync(action);
    }

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask) => await Dispatcher.DispatchAsync(funcTask);

    public async Task InvokeOnMainThreadAsync(Func<Task> funcTask)
    {
        await Dispatcher.DispatchAsync(funcTask);
    }

    public void SendInThisOrMainThreadContext(Action action, SynchronizationContext synchronizationContext = null, uint timeout = 10000)
    {
        SynchronizationContext context = synchronizationContext ?? SynchronizationContext.Current ?? MainThreadSynchronizationContext;
        context.Send(_ => action(), null);
    }

    public void SetDeadlockMonitoring(bool isEnabled)
    {
        IsDeadlockMonitoringEnabled = isEnabled;
    }
}