using FEx.Core.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Core.Abstractions.Implementations;

public class DefaultDispatcher : FExDispatcher
{
    private readonly SynchronizationContext _mainThreadSynchronizationContext;

    public DefaultDispatcher(ILogger logger,
                             IMainThreadContextProvider mainThreadContextProvider,
                             IDeadlockMonitor deadlockMonitor,
                             IStackTraceProvider stackTraceProvider,
                             IAppThreadingSettings appThreadingSettings)
        : base(logger,
            mainThreadContextProvider,
            deadlockMonitor,
            stackTraceProvider,
            appThreadingSettings.IsDeadlockMonitoringEnabled)
    {
        _mainThreadSynchronizationContext = new();
    }

    public override bool CheckAccess(object sender = null) => true; // Default implementation

    public override void BeginInvokeOnMainThread(Action action, object sender = null) =>
        _mainThreadSynchronizationContext.Post(_ => action(), null);

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object sender = null) =>
        await Task.Run(() =>
        {
            T result = default;
            _mainThreadSynchronizationContext.Send(_ => result = action(), null);

            return result;
        });

    public override async Task InvokeOnMainThreadAsync(Action action, object sender = null) =>
        await Task.Run(() => _mainThreadSynchronizationContext.Send(_ => action(), null));

    public override async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender = null) =>
        await Task.Run(async () =>
        {
            var task = Task.FromResult(default(T));
            _mainThreadSynchronizationContext.Send(_ => task = funcTask(), null);

            return await task;
        });

    public override async Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender = null) =>
        await Task.Run(async () =>
        {
            Task task = Task.CompletedTask;
            _mainThreadSynchronizationContext.Send(_ => task = funcTask(), null);
            await task;
        });

    public override void EnableCollectionSynchronization(IEnumerable collection,
                                                         object context,
                                                         Action<IEnumerable, object, Action, bool> callback)
    {
        // Default implementation - no synchronization needed for default dispatcher
    }
}