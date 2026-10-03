using FEx.Agnostics.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>Runs delegates on the main thread or the thread pool, including fire-and-forget execution tracked by task wrappers.</summary>
public interface IAsyncHelper
{
    /// <summary>Starts an action without awaiting it</summary>
    /// <param name="action">The action to run.</param>
    /// <param name="asyncMode">The thread context to run on.</param>
    /// <param name="options">How exceptions thrown by the action are handled; null uses the defaults.</param>
    /// <param name="cancellationToken">Token used to cancel the work before it starts.</param>
    /// <returns>A wrapper that tracks the task and its result.</returns>
    ITaskWrapper FireAndForget(Action action,
                               AsyncMode asyncMode = AsyncMode.Default,
                               IExceptionHandlerOptions? options = null,
                               CancellationToken cancellationToken = default);

    /// <summary>Starts a function without awaiting it</summary>
    /// <typeparam name="T">The function result type.</typeparam>
    /// <param name="func">The function to run.</param>
    /// <param name="asyncMode">The thread context to run on.</param>
    /// <param name="options">How exceptions thrown by the function are handled; null uses the defaults.</param>
    /// <param name="cancellationToken">Token used to cancel the work before it starts.</param>
    /// <returns>A wrapper that tracks the task and its result.</returns>
    ITaskWrapper<T> FireAndForget<T>(Func<T> func,
                                     AsyncMode asyncMode = AsyncMode.Default,
                                     IExceptionHandlerOptions? options = null,
                                     CancellationToken cancellationToken = default);

    /// <summary>Starts an asynchronous delegate without awaiting it</summary>
    /// <param name="task">Factory of the task to run.</param>
    /// <param name="asyncMode">The thread context to run on.</param>
    /// <param name="options">How exceptions are handled; null uses the defaults.</param>
    /// <returns>A wrapper that tracks the task and its result.</returns>
    ITaskWrapper FireTaskAndForget(Func<Task> task,
                                   AsyncMode asyncMode = AsyncMode.Default,
                                   IExceptionHandlerOptions? options = null);

    /// <summary>Starts an asynchronous delegate that returns a value without awaiting it</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="task">Factory of the task to run.</param>
    /// <param name="asyncMode">The thread context to run on.</param>
    /// <param name="options">How exceptions are handled; null uses the defaults.</param>
    /// <returns>A wrapper that tracks the task and its result.</returns>
    ITaskWrapper<T> FireTaskAndForget<T>(Func<Task<T>> task,
                                         AsyncMode asyncMode = AsyncMode.Default,
                                         IExceptionHandlerOptions? options = null);

    /// <summary>Starts several asynchronous delegates without awaiting them</summary>
    /// <param name="tasks">Factories of the tasks to run.</param>
    /// <param name="asyncMode">The thread context to run on.</param>
    /// <param name="options">How exceptions are handled; null uses the defaults.</param>
    /// <returns>One wrapper per started task.</returns>
    IReadOnlyList<ITaskWrapper> FireTasksAndForget(IEnumerable<Func<Task>> tasks,
                                                   AsyncMode asyncMode = AsyncMode.Default,
                                                   IExceptionHandlerOptions? options = null);

    /// <summary>Starts several asynchronous delegates that return values without awaiting them</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="tasks">Factories of the tasks to run.</param>
    /// <param name="asyncMode">The thread context to run on.</param>
    /// <param name="options">How exceptions are handled; null uses the defaults.</param>
    /// <returns>One wrapper per started task.</returns>
    IReadOnlyList<ITaskWrapper<T>> FireTasksAndForget<T>(IEnumerable<Func<Task<T>>> tasks,
                                                         AsyncMode asyncMode = AsyncMode.Default,
                                                         IExceptionHandlerOptions? options = null);

    /// <summary>Runs an action on the main thread and returns a task that completes when it has finished</summary>
    /// <param name="func">The action to run.</param>
    /// <param name="options">Whether the work is scheduled immediately.</param>
    /// <returns>A task that completes when the action has run.</returns>
    Task ExecuteDeferredTaskOnMainThreadAsync(Action func, AsyncOptions options = AsyncOptions.ImmediateStart);

    /// <summary>Runs a function on the main thread and returns its result</summary>
    /// <typeparam name="T">The function result type.</typeparam>
    /// <param name="func">The function to run.</param>
    /// <param name="options">Whether the work is scheduled immediately.</param>
    /// <returns>A task that yields the function result.</returns>
    Task<T> ExecuteDeferredTaskOnMainThreadAsync<T>(Func<T> func, AsyncOptions options = AsyncOptions.ImmediateStart);

    /// <summary>Runs an asynchronous delegate on the main thread and returns a task that completes when it has finished</summary>
    /// <param name="func">Factory of the task to run.</param>
    /// <param name="options">Whether the work is scheduled immediately.</param>
    /// <returns>A task that completes when the delegate's task has completed.</returns>
    Task ExecuteDeferredTaskOnMainThreadAsync(Func<Task> func, AsyncOptions options = AsyncOptions.ImmediateStart);

    /// <summary>Runs an asynchronous delegate on the main thread and returns its result</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="func">Factory of the task to run.</param>
    /// <param name="options">Whether the work is scheduled immediately.</param>
    /// <returns>A task that yields the delegate's result.</returns>
    Task<T> ExecuteDeferredTaskOnMainThreadAsync<T>(Func<Task<T>> func,
                                                    AsyncOptions options = AsyncOptions.ImmediateStart);
}