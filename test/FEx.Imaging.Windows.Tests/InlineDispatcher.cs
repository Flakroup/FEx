using FEx.Core.Abstractions.Interfaces;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace FEx.Imaging.Windows.Tests;

/// <summary>
/// Runs everything inline on the calling thread and, unlike a mocking framework, records no calls,
/// so it cannot keep the senders it is given alive.
/// </summary>
internal sealed class InlineDispatcher : IFExDispatcher
{
    public bool CheckAccess(object? sender = null) => true;

    public void BeginInvokeOnMainThread(Action action, object? sender = null) => action();

    public Task InvokeOnMainThreadAsync(Action action, object? sender = null)
    {
        action();

        return Task.CompletedTask;
    }

    public Task<T> InvokeOnMainThreadAsync<T>(Func<T> action, object? sender = null) =>
        Task.FromResult(action());

    public Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> funcTask, object? sender = null) => funcTask();

    public Task InvokeOnMainThreadAsync(Func<Task> funcTask, object? sender = null) => funcTask();

    public void InvokeOnIdleMainThread(Action action, object? sender = null) => action();

    public T InvokeOnIdleMainThread<T>(Func<T> action, object? sender = null) => action();

    public Task<T> InvokeOnIdleMainThreadAsync<T>(Func<T> action, object? sender = null) =>
        Task.FromResult(action());

    public Task InvokeOnIdleMainThreadAsync(Action action, object? sender = null)
    {
        action();

        return Task.CompletedTask;
    }

    public void InvokeOnMainThread(Action action, object? sender = null) => action();

    public T InvokeOnMainThread<T>(Func<T> action, object? sender = null) => action();

    public void EnableCollectionSynchronization(IEnumerable collection,
                                                object context,
                                                Action<IEnumerable, object, Action, bool> callback)
    {
    }

    public void SendInContext(Action action, object sender, bool useMain, uint? timeout = 3000) => action();
}
