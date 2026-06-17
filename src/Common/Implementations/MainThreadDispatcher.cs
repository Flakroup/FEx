using FEx.Core.Abstractions.Implementations;
using FEx.Core.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Common.Implementations;

// VSTHRD001: This type IS the main-thread dispatcher primitive - it switches to the UI thread via
// SynchronizationContext.Post/Send (overriding FExDispatcher). JoinableTaskFactory is not FEx's
// concurrency model, so the legacy thread-switching APIs are intentional here.
#pragma warning disable VSTHRD001

public class MainThreadDispatcher : FExDispatcher
{
    public MainThreadDispatcher(IMainThreadContextProvider mainThreadContextProvider,
                                ILogger logger,
                                IDeadlockMonitor deadlockMonitor,
                                IAppThreadingSettings appInstanceSettings,
                                IStackTraceProvider stackTraceProvider)
        : base(logger,
            mainThreadContextProvider,
            deadlockMonitor,
            stackTraceProvider,
            appInstanceSettings.IsDeadlockMonitoringEnabled)
    {
    }

    public override bool CheckAccess(object sender = null) => _mainThreadContextProvider.Thread == Thread.CurrentThread;

    public override void BeginInvokeOnMainThread(Action action, object sender = null) =>
        _mainThreadContextProvider.Context?.Post(_ => action(), null);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object sender = null) =>
        await Task.Run(() =>
        {
            T result = default;
            _mainThreadContextProvider.Context?.Send(_ => result = action(), null);

            return result;
        });

    public override async Task InvokeOnMainThreadAsync(Action action, object sender = null) =>
        await Task.Run(() => _mainThreadContextProvider.Context?.Send(_ => action(), null));

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender = null) =>
        await Task.Run(async () =>
        {
            var task = Task.FromResult(default(T));
            _mainThreadContextProvider.Context?.Send(_ => task = funcTask(), null);

            return await task;
        });

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender = null) =>
        await Task.Run(async () =>
        {
            var task = Task.CompletedTask;
            _mainThreadContextProvider.Context?.Send(_ => task = funcTask(), null);
            await task;
        });

    public override void EnableCollectionSynchronization(IEnumerable collection,
                                                         object context,
                                                         Action<IEnumerable, object, Action, bool> callback)
    {
        // Default implementation - no synchronization needed for main thread dispatcher
    }
}