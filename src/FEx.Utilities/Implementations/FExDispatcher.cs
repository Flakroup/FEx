using FEx.Abstractions;
using FEx.Basics;
using FEx.Fundamentals;
using FEx.Utilities.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Utilities.Implementations;

public abstract class FExDispatcher : IFExDispatcher
{
    protected static SynchronizationContext MainThreadSynchronizationContext =>
        Foundation.MainSynchronizationContext;

    private readonly ILogger _logger;

    public bool IsDeadlockMonitoringEnabled { get; private set; }

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
                                              int? timeout = 3000)
    {
        SynchronizationContext syncContext = synchronizationContext ?? MainThreadSynchronizationContext;
        Timer timer = IsDeadlockMonitoringEnabled && timeout.HasValue
            ? new Timer(Callback, FExBasics.StackTraceProvider.GetStackTrace(), timeout.Value, Timeout.Infinite)
            : null;
        try
        {
            syncContext.Send(_ => action(), default);
        }
        finally
        {
            timer?.Change(Timeout.Infinite, Timeout.Infinite);
            timer?.Dispose();
        }
    }

    public void SetDeadlockMonitoring(bool isEnabled)
    {
        IsDeadlockMonitoringEnabled = isEnabled;
    }

    private void Callback(object state)
    {
        var stackTrace = (StackTrace)state;
        var ex = new AttachedException("Deadlock assumed, as no action could've been performed during timeout.",
            stackTrace);
        _logger.LogError(ex, ex.Message);
        throw ex;
    }
}