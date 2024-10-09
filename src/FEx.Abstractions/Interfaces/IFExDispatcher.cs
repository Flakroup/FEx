using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Abstractions.Interfaces;

public interface IFExDispatcher
{
    void BeginInvokeOnMainThread(Action action);
    bool CheckAccess(object sender = null);

    void InvokeOnIdleMainThread(Action action, object sender = null);
    T InvokeOnIdleMainThread<T>(Func<T> action, object sender = null);

    void InvokeOnMainThread(Action action, object sender = null);
    T InvokeOnMainThread<T>(Func<T> action, object sender = null);

    Task InvokeOnIdleMainThreadAsync(Action action, object sender = null);
    Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object sender = null);

    Task InvokeOnMainThreadAsync(Action action, object sender = null);
    Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object sender = null);
    Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object sender = null);
    Task InvokeOnMainThreadAsync(Func<Task> funcTask, object sender = null);

    /// <summary>
    ///     Invokes action delegate in current, instance initialization or main thread <see cref="SynchronizationContext" />
    /// depending on which is not null first
    /// </summary>
    /// <param name="action">Action to invoke</param>
    /// <param name="sender">Action origin object</param>
    /// <param name="timeout">Deadlock timeout</param>
    void SendInContext(Action action, object sender, uint? timeout = 10000);
}