using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IUIContextAware
{
    void BeginInvokeOnMainThread(Action action);
    bool CheckAccess();

    void InvokeOnIdleMainThread(Action action);
    T InvokeOnIdleMainThread<T>(Func<T> action);

    void InvokeOnMainThread(Action action);
    T InvokeOnMainThread<T>(Func<T> action);

    Task InvokeOnIdleMainThreadAsync(Action action);
    Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action);

    Task InvokeOnMainThreadAsync(Action action);
    Task<T> InvokeOnMainThreadAsync<T>(Func<T> action);
    Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask);
    Task InvokeOnMainThreadAsync(Func<Task> funcTask);
    void SendInThisOrMainThreadContext(Action action,
                                       SynchronizationContext synchronizationContext = null,
                                       uint timeout = 10000);
}