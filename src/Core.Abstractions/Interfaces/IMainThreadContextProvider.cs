using System;
using System.Threading;

namespace FEx.Core.Abstractions.Interfaces;

public interface IMainThreadContextProvider
{
    event EventHandler<EventArgs> ThreadHasChanged;
    Thread Thread { get; }
    bool IsDispatcherContext { get; set; }
    SynchronizationContext Context { get; }

    void SetMainThread(bool throwOnNonMainThread = true);
}

/*public interface IMainThreadContextProvider
{
    SynchronizationContext Context { get; }

    void BeginInvokeOnMainThread(Action action);
    Task<T> InvokeOnMainThreadAsync<T>(Func<T> func);
    Task InvokeOnMainThreadAsync(Action action);
    Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask);
    Task InvokeOnMainThreadAsync(Func<Task> funcTask);

    void EnableCollectionSynchronization(IEnumerable collection,
                                         object context,
                                         Action<IEnumerable, object, Action, bool> callback);
}*/