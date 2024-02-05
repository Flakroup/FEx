using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Abstractions.Interfaces;

public interface IFExDispatcher
{
    void BeginInvokeOnMainThread(Action action);
    Task<T> InvokeOnMainThreadAsync<T>(Func<T> func);
    Task InvokeOnMainThreadAsync(Action action);
    Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask);
    Task InvokeOnMainThreadAsync(Func<Task> funcTask);

    void SendInThisOrMainThreadContext(Action action,
                                       SynchronizationContext synchronizationContext = null,
                                       uint timeout = 10000);
}