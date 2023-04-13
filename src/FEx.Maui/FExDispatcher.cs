using FEx.Abstractions;
using FEx.Fundamentals;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Maui;

public class FExDispatcher : BindableObject, IFExDispatcher
{
    public SynchronizationContext MainThreadSynchronizationContext { get; private set; }

    public bool IsDeadlockMonitoringEnabled { get; private set; }

    public FExDispatcher()
    {
        if (MainThread.IsMainThread)
            MainThreadSynchronizationContext = SynchronizationContext.Current;
        else
            Foundation.AsyncHelper.FireTaskAndForget(async () => MainThreadSynchronizationContext = await MainThread.GetMainThreadSynchronizationContextAsync());
    }

    public void BeginInvokeOnMainThread(Action action)
    {
        Dispatcher.Dispatch(action);
    }

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func)
    {
        return await Dispatcher.DispatchAsync(func);
    }

    public async Task InvokeOnMainThreadAsync(Action action)
    {
        await Dispatcher.DispatchAsync(action);
    }

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask)
    {
        return await Dispatcher.DispatchAsync(funcTask);
    }

    public async Task InvokeOnMainThreadAsync(Func<Task> funcTask)
    {
        await Dispatcher.DispatchAsync(funcTask);
    }

    public void EnableCollectionSynchronization(IEnumerable collection, object context, Action<IEnumerable, object, Action, bool> callback)
    {
        throw new NotImplementedException();
    }

    public void SendInThisOrMainThreadContext(Action action, SynchronizationContext synchronizationContext = null, int? timeout = 3000)
    {
        SynchronizationContext context = synchronizationContext ?? SynchronizationContext.Current ?? MainThreadSynchronizationContext;
        context.Send(_ => action(), null);
    }

    public void SetDeadlockMonitoring(bool isEnabled)
    {
        IsDeadlockMonitoringEnabled = isEnabled;
    }
}