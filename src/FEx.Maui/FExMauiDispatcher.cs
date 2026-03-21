using FEx.Core.Abstractions.Implementations;
using FEx.Core.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Dispatching;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace FEx.Maui;

public class FExMauiDispatcher : FExDispatcher
{
    private readonly IDispatcher _dispatcher;

    public FExMauiDispatcher(ILogger logger,
                             IMainThreadContextProvider mainThreadContextProvider,
                             IDeadlockMonitor deadlockMonitor,
                             IStackTraceProvider stackTraceProvider)
        : this(logger, mainThreadContextProvider, deadlockMonitor, stackTraceProvider, false)
    {
    }

    public FExMauiDispatcher(ILogger logger,
                             IMainThreadContextProvider mainThreadContextProvider,
                             IDeadlockMonitor deadlockMonitor,
                             IStackTraceProvider stackTraceProvider,
                             bool isDeadlockMonitoringEnabled)
        : base(logger, mainThreadContextProvider, deadlockMonitor, stackTraceProvider, isDeadlockMonitoringEnabled)
    {
        _dispatcher = Dispatcher.GetForCurrentThread();
    }

    public override void BeginInvokeOnMainThread(Action action, object sender) => _dispatcher.Dispatch(action);

    public override async Task InvokeOnMainThreadAsync(Action action, object sender) =>
        await _dispatcher.DispatchAsync(action);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func, object sender) =>
        await _dispatcher.DispatchAsync(func);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender) =>
        await _dispatcher.DispatchAsync(funcTask);

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender) =>
        await _dispatcher.DispatchAsync(funcTask);

    public override bool CheckAccess(object sender) => _dispatcher.IsDispatchRequired;

    public override void EnableCollectionSynchronization(IEnumerable collection,
                                                         object context,
                                                         Action<IEnumerable, object, Action, bool> callback)
    {
        // MAUI doesn't need special collection synchronization
    }
}