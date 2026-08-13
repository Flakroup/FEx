using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions.Implementations;
using FEx.Core.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Dispatching;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace FEx.Maui;

[SuppressMessage("ReSharper", "OptionalParameterHierarchyMismatch")]
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
        // MAUI dispatcher must exist on the thread that constructs this type; fail fast if it does not.
        _dispatcher = Dispatcher.GetForCurrentThread().Guard(nameof(Dispatcher));
    }

    public override void BeginInvokeOnMainThread(Action action, object? sender = null) => _dispatcher.Dispatch(action);

    public override async Task InvokeOnMainThreadAsync(Action action, object? sender = null) =>
        await _dispatcher.DispatchAsync(action);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func, object? sender = null) =>
        await _dispatcher.DispatchAsync(func);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object? sender = null) =>
        await _dispatcher.DispatchAsync(funcTask);

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object? sender = null) =>
        await _dispatcher.DispatchAsync(funcTask);

    public override bool CheckAccess(object? sender = null) => _dispatcher.IsDispatchRequired;

    public override void EnableCollectionSynchronization(IEnumerable collection,
                                                         object context,
                                                         Action<IEnumerable, object, Action, bool> callback)
    {
        // MAUI doesn't need special collection synchronization
    }
}