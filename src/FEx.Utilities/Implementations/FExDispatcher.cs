using FEx.Abstractions;
using FEx.Basics.Helpers;
using FEx.Fundamentals;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Utilities.Implementations;

public abstract class FExDispatcher : IFExDispatcher
{
    private readonly ILogger _logger;

    protected static SynchronizationContext MainThreadSynchronizationContext =>
        Foundation.MainSynchronizationContext;

    protected FExDispatcher(ILogger logger)
    {
        _logger = logger;
    }

    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<T> func);

    public abstract Task InvokeOnMainThreadAsync(Action action);

    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask);

    public abstract Task InvokeOnMainThreadAsync(Func<Task> funcTask);

    public abstract void BeginInvokeOnMainThread(Action action);

    public void SendInThisOrMainThreadContext(Action action,
                                              SynchronizationContext synchronizationContext = null,
                                              uint timeout = 10000)
    {
        SynchronizationContext syncContext = synchronizationContext ?? MainThreadSynchronizationContext;
        DeadlockMonitor.Execute(() => syncContext.Send(_ => action(), default), timeout);
    }
}