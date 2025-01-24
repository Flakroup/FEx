using FEx.Abstractions.Interfaces;
using FEx.Basics.Exceptions;
using FEx.Common.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Basics.Abstractions;

public abstract class FExDispatcher : IFExDispatcher
{
    protected readonly ILogger _logger;
    protected readonly IMainThreadContextProvider _mainThreadContextProvider;
    protected readonly IDeadlockMonitor _deadlockMonitor;
    protected readonly IStackTraceProvider _stackTraceProvider;
    protected readonly SynchronizationContext _ctorSynchronizationContext;
    private readonly bool _isDeadlockMonitoringEnabled;

    protected SynchronizationContext MainThreadSynchronizationContext => _mainThreadContextProvider.Context;

    protected FExDispatcher(ILogger logger,
                            IMainThreadContextProvider mainThreadContextProvider,
                            IDeadlockMonitor deadlockMonitor,
                            IStackTraceProvider stackTraceProvider,
                            bool isDeadlockMonitoringEnabled = false)
    {
        _ctorSynchronizationContext = SynchronizationContext.Current;

        _logger = logger;
        _mainThreadContextProvider = mainThreadContextProvider;
        _deadlockMonitor = deadlockMonitor;
        _stackTraceProvider = stackTraceProvider;

        _isDeadlockMonitoringEnabled = isDeadlockMonitoringEnabled;
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

    /// <inheritdoc />
    public virtual void SendInContext(Action action, object sender, uint? timeout = 3000)
    {
        bool isInCtorOrMainContext = IsInCreationContext() || IsInMainContext();

        if (isInCtorOrMainContext)
        {
            action();

            return;
        }

        StackTrace stackTrace = _stackTraceProvider.GetStackTrace();

        if (!_isDeadlockMonitoringEnabled
            || !timeout.HasValue)
        {
            SendInThisOrMainThreadContextCore(action, sender, stackTrace);

            return;
        }

        _deadlockMonitor.Execute(() => SendInThisOrMainThreadContextCore(action, sender, stackTrace),
            stackTrace,
            timeout.Value);
    }

    private bool IsInCreationContext() =>
        _ctorSynchronizationContext is null && SynchronizationContext.Current is null
        || _ctorSynchronizationContext is not null
        && SynchronizationContext.Current is not null
        && _ctorSynchronizationContext.Equals(SynchronizationContext.Current);

    private bool IsInMainContext() =>
        MainThreadSynchronizationContext is null && SynchronizationContext.Current is null
        || MainThreadSynchronizationContext is not null
        && SynchronizationContext.Current is not null
        && MainThreadSynchronizationContext.Equals(SynchronizationContext.Current);

    private void SendInThisOrMainThreadContextCore(Action action, object sender, StackTrace stackTrace)
    {
        SynchronizationContext context = _ctorSynchronizationContext ?? MainThreadSynchronizationContext;

        try
        {
#pragma warning disable VSTHRD001
            context.Send(_ => action(), null);
#pragma warning restore VSTHRD001
        }
        catch (Exception ex)
        {
            _logger.LogError(new AttachedException(sender, stackTrace, ex));

            throw;
        }
    }
}