using FEx.Abstractions;
using FEx.Utilities.Exceptions;
using FEx.Utilities.Interfaces;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Utilities.Implementations;

public abstract class FExDispatcher : IFExDispatcher
{
    protected SynchronizationContext _mainThreadSynchronizationContext;
    private readonly ILogger _logger;

    public bool IsDeadlockMonitoringEnabled { get; private set; }

    public FExDispatcher(ILogger logger)
    {
        _logger = logger;
        _mainThreadSynchronizationContext = SynchronizationContext.Current;
    }

    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<T> func);

    public abstract Task InvokeOnMainThreadAsync(Action action);

    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask);

    public abstract Task InvokeOnMainThreadAsync(Func<Task> funcTask);

    public abstract void EnableCollectionSynchronization(IEnumerable collection, object context, Action<IEnumerable, object, Action, bool> callback);

    public abstract void BeginInvokeOnMainThread(Action action);

    public void SendInThisOrMainThreadContext(Action action, SynchronizationContext synchronizationContext = null, int? timeout = 3000)
    {
        SynchronizationContext syncContext = synchronizationContext ?? _mainThreadSynchronizationContext;
        Timer timer = IsDeadlockMonitoringEnabled && timeout.HasValue
            ? new Timer(Callback, Fundamentals.StackTraceGenerator.GetCachedStackTrace(), timeout.Value, Timeout.Infinite)
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
        var ex = new AttachedException("Deadlock assumed, as no action could've been performed during timeout.", stackTrace);
        _logger.LogError(ex);
        throw ex;
    }
}