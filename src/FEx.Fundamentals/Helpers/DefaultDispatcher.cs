using FEx.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Fundamentals.Helpers;

public class DefaultDispatcher : IFExDispatcher
{
    public void BeginInvokeOnMainThread(Action action)
    {
        throw new NotImplementedException();
    }

    public Task<T> InvokeOnMainThreadAsync<T>(Func<T> func) => throw new NotImplementedException();


    public Task InvokeOnMainThreadAsync(Action action) => throw new NotImplementedException();

    public Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask) => throw new NotImplementedException();

    public Task InvokeOnMainThreadAsync(Func<Task> funcTask) => throw new NotImplementedException();

    public void SendInThisOrMainThreadContext(Action action,
                                              SynchronizationContext synchronizationContext = null,
                                              uint timeout = 10000)
    {
        throw new NotImplementedException();
    }
}