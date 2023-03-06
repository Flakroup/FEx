using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Abstractions;

public interface IFExDispatcher
{
    bool IsDeadlockMonitoringEnabled { get; }

    void BeginInvokeOnMainThread(Action action);
    Task<T> InvokeOnMainThreadAsync<T>(Func<T> func);
    Task InvokeOnMainThreadAsync(Action action);
    Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask);
    Task InvokeOnMainThreadAsync(Func<Task> funcTask);
    void EnableCollectionSynchronization(IEnumerable collection, object context, Action<IEnumerable, object, Action, bool> callback);
    void SendInThisOrMainThreadContext(Action action, SynchronizationContext synchronizationContext = null, int? timeout = 3000);
    void SetDeadlockMonitoring(bool isEnabled);
}