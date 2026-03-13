using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Core.Abstractions.Implementations;

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
    public abstract void BeginInvokeOnMainThread(Action action, object sender = null);
    public abstract Task InvokeOnMainThreadAsync(Action action, object sender = null);
    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object sender = null);
    public abstract Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender = null);
    public abstract Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender = null);

    public virtual void InvokeOnIdleMainThread(Action action, object sender = null) =>
        //this implementation is not able to determine wherever UI context is idle
        InvokeOnMainThread(action, sender);

    public virtual T InvokeOnIdleMainThread<T>(Func<T> action, object sender = null) =>
        //this implementation is not able to determine wherever UI context is idle
        InvokeOnMainThread(action, sender);

    public virtual async Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object sender = null) =>
        //this implementation is not able to determine wherever UI context is idle
        await InvokeOnMainThreadAsync(action, sender);

    public virtual async Task InvokeOnIdleMainThreadAsync(Action action, object sender = null) =>
        //this implementation is not able to determine wherever UI context is idle
        await InvokeOnMainThreadAsync(action, sender);

    public virtual void InvokeOnMainThread(Action action, object sender = null)
    {
        var context = sender is Thread thread
            ? thread.GetThreadSynchronizationContext()
            : _mainThreadContextProvider.Context;

        if (CheckAccess(sender)
            || context is null)
            action();
        else
            context.SendInContext(sender, action);
    }

    public virtual T InvokeOnMainThread<T>(Func<T> action, object sender = null)
    {
        var context = sender is Thread thread
            ? thread.GetThreadSynchronizationContext()
            : _mainThreadContextProvider.Context;

        return CheckAccess(sender) || context is null
            ? action()
            : context.SendInContext(sender, action);
    }

    public virtual void EnableCollectionSynchronization(IEnumerable collection,
                                                        object context,
                                                        Action<IEnumerable, object, Action, bool> callback)
    {
        // Default implementation - override in derived classes if needed
    }

    /// <inheritdoc />
    public virtual void SendInContext(Action action, object sender, uint? timeout = 3000)
    {
        var isInCtorOrMainContext = IsInCreationContext() || IsInMainContext();

        if (isInCtorOrMainContext)
        {
            action();

            return;
        }

        var stackTrace = _stackTraceProvider.GetStackTrace();

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
        var context = _ctorSynchronizationContext ?? MainThreadSynchronizationContext;

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