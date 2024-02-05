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

    protected static SynchronizationContext MainThreadSynchronizationContext =>
        FExBasics.MainSynchronizationContext;

    protected FExDispatcher(ILogger logger)
    {
        _logger = logger;
    }

    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<T> func);

    public abstract Task InvokeOnMainThreadAsync(Action action);

    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask);

    public abstract Task InvokeOnMainThreadAsync(Func<Task> funcTask);

    public abstract void BeginInvokeOnMainThread(Action action);

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