using FEx.Abstractions.Interfaces;
using FEx.Basics.Abstractions;
using FEx.Common.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Dispatching;
using System;
using System.Threading.Tasks;

namespace FEx.Maui;

public class FExMauiDispatcher : FExDispatcher
{
    private readonly IDispatcher _dispatcher;

    public FExMauiDispatcher(ILogger logger,
                             IMainThreadContextProvider mainThreadContextProvider,
                             IDeadlockMonitor deadlockMonitor,
                             IStackTraceProvider stackTraceProvider)
        : base(logger, mainThreadContextProvider, deadlockMonitor, stackTraceProvider)
    {
        _dispatcher = Dispatcher.GetForCurrentThread();
    }

    public override void BeginInvokeOnMainThread(Action action) => _dispatcher.Dispatch(action);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func, object sender = null) =>
        await _dispatcher.DispatchAsync(func);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender = null) =>
        await _dispatcher.DispatchAsync(funcTask);

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender = null) =>
        await _dispatcher.DispatchAsync(funcTask);

    public override bool CheckAccess(object sender = null) => _dispatcher.IsDispatchRequired;

    public override void InvokeOnIdleMainThread(Action action, object sender = null) => _dispatcher.Dispatch(action);

    public override T InvokeOnIdleMainThread<T>(Func<T> action, object sender = null)
    {
        T result = default;
        _dispatcher.Dispatch(() => result = action());

        return result;
    }

    public override void InvokeOnMainThread(Action action, object sender = null) => _dispatcher.Dispatch(action);

    public override T InvokeOnMainThread<T>(Func<T> action, object sender = null)
    {
        T result = default;
        _dispatcher.Dispatch(() => result = action());

        return result;
    }

    public override async Task InvokeOnIdleMainThreadAsync(Action action, object sender = null) =>
        await _dispatcher.DispatchAsync(action);

    public override async Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object sender = null) =>
        await _dispatcher.DispatchAsync(action);

    public override async Task InvokeOnMainThreadAsync(Action action, object sender = null) =>
        await _dispatcher.DispatchAsync(action);
}