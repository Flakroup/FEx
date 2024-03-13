using FEx.Abstractions.Interfaces;
using FEx.Basics.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Basics.Abstractions;

public abstract class FExDispatcher : IFExDispatcher
{
    protected readonly ILogger _logger;

    protected static SynchronizationContext MainThreadSynchronizationContext => FExBasics.MainSynchronizationContext;

    protected FExDispatcher(ILogger logger)
    {
        _logger = logger;
    }

    public abstract bool CheckAccess(object sender = null);
    public abstract void BeginInvokeOnMainThread(Action action);
    public abstract void InvokeOnIdleMainThread(Action action, object sender = null);
    public abstract T InvokeOnIdleMainThread<T>(Func<T> action, object sender = null);
    public abstract void InvokeOnMainThread(Action action, object sender = null);
    public abstract T InvokeOnMainThread<T>(Func<T> action, object sender = null);
    public abstract Task InvokeOnIdleMainThreadAsync(Action action, object sender = null);
    public abstract Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object sender = null);
    public abstract Task InvokeOnMainThreadAsync(Action action, object sender = null);
    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object sender = null);
    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender = null);
    public abstract Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender = null);

    public virtual void SendInThisOrMainThreadContext(Action action,
                                                      SynchronizationContext synchronizationContext = null,
                                                      uint timeout = 10000)
    {
        SynchronizationContext syncContext = synchronizationContext ?? MainThreadSynchronizationContext;
#pragma warning disable VSTHRD001
        DeadlockMonitor.Execute(() => syncContext.Send(_ => action(), default), timeout);
#pragma warning restore VSTHRD001
    }
}