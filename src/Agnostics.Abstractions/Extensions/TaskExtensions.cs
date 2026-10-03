using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for running, awaiting and inspecting tasks and for fire-and-forget execution through <see cref="IAsyncHelper" />.</summary>
public static class TaskExtensions
{
    private static IAsyncHelper AsyncHelper => FExAgnosticsStatics.AsyncHelper;

    /// <summary>Runs all actions and waits for them to complete.</summary>
    /// <param name="tasks">The actions to run.</param>
    /// <param name="mode">The thread context to run on.</param>
    /// <param name="options">How the work is started.</param>
    /// <param name="cancellationToken">Token used to cancel actions that have not started.</param>
    public static async Task WhenAllAsync(this IEnumerable<Action> tasks,
                                          AsyncMode mode = AsyncMode.Default,
                                          AsyncOptions options = AsyncOptions.ImmediateStart,
                                          CancellationToken cancellationToken = default) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options, cancellationToken)).WhenAllAsync();

    /// <summary>Runs all functions and waits for them to complete.</summary>
    /// <typeparam name="T">The function result type.</typeparam>
    /// <param name="tasks">The functions to run.</param>
    /// <param name="mode">The thread context to run on.</param>
    /// <param name="options">How the work is started.</param>
    /// <param name="cancellationToken">Token used to cancel functions that have not started.</param>
    /// <returns>The results in the order of <paramref name="tasks" />.</returns>
    public static async Task<T[]> WhenAllAsync<T>(this IEnumerable<Func<T>> tasks,
                                                  AsyncMode mode = AsyncMode.Default,
                                                  AsyncOptions options = AsyncOptions.ImmediateStart,
                                                  CancellationToken cancellationToken = default) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options, cancellationToken)).WhenAllAsync();

    /// <summary>Starts all task factories and waits for the tasks to complete.</summary>
    /// <param name="tasks">The task factories.</param>
    /// <param name="mode">The thread context to run on.</param>
    /// <param name="options">How the work is started.</param>
    public static async Task WhenAllTasksAsync(this IEnumerable<Func<Task>> tasks,
                                               AsyncMode mode = AsyncMode.Default,
                                               AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options)).WhenAllAsync();

    /// <summary>Starts all task factories and waits for the tasks to complete.</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="tasks">The task factories.</param>
    /// <param name="mode">The thread context to run on.</param>
    /// <param name="options">How the work is started.</param>
    /// <returns>The results in the order of <paramref name="tasks" />.</returns>
    public static async Task<T[]> WhenAllTasksAsync<T>(this IEnumerable<Func<Task<T>>> tasks,
                                                       AsyncMode mode = AsyncMode.Default,
                                                       AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options)).WhenAllAsync();

    /// <summary>Runs an action for every value and waits for all of them to complete.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values to process.</param>
    /// <param name="asyncAction">The action to run per value.</param>
    /// <param name="mode">The thread context to run on.</param>
    /// <param name="options">How the work is started.</param>
    /// <param name="cancellationToken">Token used to cancel actions that have not started.</param>
    public static async Task WithWhenAllAsync<T>(this IEnumerable<T> values,
                                                 Action<T> asyncAction,
                                                 AsyncMode mode = AsyncMode.Default,
                                                 AsyncOptions options = AsyncOptions.ImmediateStart,
                                                 CancellationToken cancellationToken = default)
    {
        if (options.HasFlagFast(AsyncOptions.ImmediateStart))
        {
            await values.Select<T, Func<Task>>(v => () => Task.Run(() => asyncAction(v), cancellationToken))
                .WhenAllTasksAsync(mode);

            return;
        }

        await values.Select<T, Action>(v => () => asyncAction(v)).WhenAllAsync(mode, options, cancellationToken);
    }

    /// <summary>Runs a function for every value and waits for all of them to complete.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TResult">The function result type.</typeparam>
    /// <param name="values">The values to process.</param>
    /// <param name="asyncAction">The function to run per value.</param>
    /// <param name="mode">The thread context to run on.</param>
    /// <param name="options">How the work is started.</param>
    /// <param name="cancellationToken">Token used to cancel functions that have not started.</param>
    /// <returns>The results in the order of <paramref name="values" />.</returns>
    public static async Task<TResult[]> WithWhenAllAsync<T, TResult>(this IEnumerable<T> values,
                                                                     Func<T, TResult> asyncAction,
                                                                     AsyncMode mode = AsyncMode.Default,
                                                                     AsyncOptions options = AsyncOptions.ImmediateStart,
                                                                     CancellationToken cancellationToken = default)
    {
        if (options.HasFlagFast(AsyncOptions.ImmediateStart))
            return await values
                .Select<T, Func<Task<TResult>>>(v => () => Task.Run(() => asyncAction(v), cancellationToken))
                .WhenAllTasksAsync(mode);

        return await values.Select<T, Func<TResult>>(v => () => asyncAction(v))
            .WhenAllAsync(mode, options, cancellationToken);
    }

    /// <summary>Runs an asynchronous delegate for every value and waits for all tasks to complete.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values to process.</param>
    /// <param name="asyncAction">The asynchronous delegate to run per value.</param>
    /// <param name="mode">The thread context to run on.</param>
    /// <param name="options">How the work is started.</param>
    public static async Task WithWhenAllTasksAsync<T>(this IEnumerable<T> values,
                                                      Func<T, Task> asyncAction,
                                                      AsyncMode mode = AsyncMode.Default,
                                                      AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await values.Select<T, Func<Task>>(v => () => asyncAction(v)).WhenAllTasksAsync(mode, options);

    /// <summary>Runs an asynchronous delegate for every value and waits for all tasks to complete.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TResult">The task result type.</typeparam>
    /// <param name="values">The values to process.</param>
    /// <param name="asyncAction">The asynchronous delegate to run per value.</param>
    /// <param name="mode">The thread context to run on.</param>
    /// <param name="options">How the work is started.</param>
    /// <returns>The results in the order of <paramref name="values" />.</returns>
    public static async Task<TResult[]> WithWhenAllTasksAsync<T, TResult>(this IEnumerable<T> values,
                                                                          Func<T, Task<TResult>> asyncAction,
                                                                          AsyncMode mode = AsyncMode.Default,
                                                                          AsyncOptions options =
                                                                              AsyncOptions.ImmediateStart) =>
        await values.Select<T, Func<Task<TResult>>>(v => () => asyncAction(v)).WhenAllTasksAsync(mode, options);

    /// <summary>Fires an action on the main thread without awaiting it.</summary>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="action">The action to run.</param>
    /// <param name="cancellationToken">Token used to cancel the work before it starts.</param>
    /// <returns>A wrapper tracking the task.</returns>
    public static ITaskWrapper FireOnMainThreadAndForget(this IAsyncHelper asyncHelper,
                                                         Action action,
                                                         CancellationToken cancellationToken = default) =>
        asyncHelper.FireAndForget(action, AsyncMode.MainThread, cancellationToken: cancellationToken);

    /// <summary>Fires a function on the main thread without awaiting it.</summary>
    /// <typeparam name="T">The function result type.</typeparam>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="func">The function to run.</param>
    /// <param name="cancellationToken">Token used to cancel the work before it starts.</param>
    /// <returns>A wrapper tracking the task and its result.</returns>
    public static ITaskWrapper<T> FireOnMainThreadAndForget<T>(this IAsyncHelper asyncHelper,
                                                               Func<T> func,
                                                               CancellationToken cancellationToken = default) =>
        asyncHelper.FireAndForget(func, AsyncMode.MainThread, cancellationToken: cancellationToken);

    /// <summary>Fires an asynchronous delegate on the main thread without awaiting it.</summary>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="task">Factory of the task to run.</param>
    /// <returns>A wrapper tracking the task.</returns>
    public static ITaskWrapper FireTaskOnMainThreadAndForget(this IAsyncHelper asyncHelper, Func<Task> task) =>
        asyncHelper.FireTaskAndForget(task, AsyncMode.MainThread);

    /// <summary>Fires an asynchronous delegate on the main thread without awaiting it.</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="task">Factory of the task to run.</param>
    /// <returns>A wrapper tracking the task and its result.</returns>
    public static ITaskWrapper<T> FireTaskOnMainThreadAndForget<T>(this IAsyncHelper asyncHelper, Func<Task<T>> task) =>
        asyncHelper.FireTaskAndForget(task, AsyncMode.MainThread);

    /// <summary>Fires several asynchronous delegates on the main thread without awaiting them.</summary>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="tasks">Factories of the tasks to run.</param>
    /// <returns>One wrapper per started task.</returns>
    public static IReadOnlyList<ITaskWrapper> FireTasksOnMainThreadAndForget(this IAsyncHelper asyncHelper,
                                                                             IEnumerable<Func<Task>> tasks) =>
        asyncHelper.FireTasksAndForget(tasks, AsyncMode.MainThread);

    /// <summary>Fires several asynchronous delegates on the main thread without awaiting them.</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="tasks">Factories of the tasks to run.</param>
    /// <returns>One wrapper per started task.</returns>
    public static IReadOnlyList<ITaskWrapper<T>> FireTasksOnMainThreadAndForget<T>(this IAsyncHelper asyncHelper,
        IEnumerable<Func<Task<T>>> tasks) =>
        asyncHelper.FireTasksAndForget(tasks, AsyncMode.MainThread);

    /// <summary>Fires an action on the thread pool without awaiting it.</summary>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="action">The action to run.</param>
    /// <param name="cancellationToken">Token used to cancel the work before it starts.</param>
    /// <returns>A wrapper tracking the task.</returns>
    public static ITaskWrapper FireOnThreadPoolAndForget(this IAsyncHelper asyncHelper,
                                                         Action action,
                                                         CancellationToken cancellationToken = default) =>
        asyncHelper.FireAndForget(action, AsyncMode.ThreadPool, cancellationToken: cancellationToken);

    /// <summary>Fires a function on the thread pool without awaiting it.</summary>
    /// <typeparam name="T">The function result type.</typeparam>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="func">The function to run.</param>
    /// <param name="cancellationToken">Token used to cancel the work before it starts.</param>
    /// <returns>A wrapper tracking the task and its result.</returns>
    public static ITaskWrapper<T> FireOnThreadPoolAndForget<T>(this IAsyncHelper asyncHelper,
                                                               Func<T> func,
                                                               CancellationToken cancellationToken = default) =>
        asyncHelper.FireAndForget(func, AsyncMode.ThreadPool, cancellationToken: cancellationToken);

    /// <summary>Fires an asynchronous delegate on the thread pool without awaiting it.</summary>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="task">Factory of the task to run.</param>
    /// <returns>A wrapper tracking the task.</returns>
    public static ITaskWrapper FireTaskOnThreadPoolAndForget(this IAsyncHelper asyncHelper, Func<Task> task) =>
        asyncHelper.FireTaskAndForget(task, AsyncMode.ThreadPool);

    /// <summary>Fires an asynchronous delegate on the thread pool without awaiting it.</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="task">Factory of the task to run.</param>
    /// <returns>A wrapper tracking the task and its result.</returns>
    public static ITaskWrapper<T> FireTaskOnThreadPoolAndForget<T>(this IAsyncHelper asyncHelper, Func<Task<T>> task) =>
        asyncHelper.FireTaskAndForget(task, AsyncMode.ThreadPool);

    /// <summary>Fires several asynchronous delegates on the thread pool without awaiting them.</summary>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="tasks">Factories of the tasks to run.</param>
    /// <returns>One wrapper per started task.</returns>
    public static IReadOnlyList<ITaskWrapper> FireTasksOnThreadPoolAndForget(this IAsyncHelper asyncHelper,
                                                                             IEnumerable<Func<Task>> tasks) =>
        asyncHelper.FireTasksAndForget(tasks, AsyncMode.ThreadPool);

    /// <summary>Fires several asynchronous delegates on the thread pool without awaiting them.</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="asyncHelper">The helper that runs the work.</param>
    /// <param name="tasks">Factories of the tasks to run.</param>
    /// <returns>One wrapper per started task.</returns>
    public static IReadOnlyList<ITaskWrapper<T>> FireTasksOnThreadPoolAndForget<T>(this IAsyncHelper asyncHelper,
        IEnumerable<Func<Task<T>>> tasks) =>
        asyncHelper.FireTasksAndForget(tasks, AsyncMode.ThreadPool);

    /// <summary>
    /// Waits for task to start.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <returns>
    /// Task
    /// </returns>
    public static async Task WaitForTaskToStartAsync(this Task task) =>
        await AsyncStatics.DelayUntilAsync(task.IsNotStarted, milliseconds: 1);

    /// <summary>Waits, polling every millisecond, until a task has finished (completed, faulted or canceled).</summary>
    /// <param name="task">The task to wait for.</param>
    /// <returns>A task that completes once <paramref name="task" /> has finished.</returns>
    public static async Task WaitForTaskToEndAsync(this Task task) =>
        await AsyncStatics.DelayUntilAsync(() => !task.IsFinished(), milliseconds: 1);

    /// <summary>Determines whether a task is waiting for activation, waiting to run, running or waiting for children.</summary>
    /// <param name="task">The task to inspect.</param>
    /// <returns><c>true</c> if the task is in progress; <c>false</c> when it is not or is null.</returns>
    public static bool IsRunning(this Task task) =>
        task?.Status is TaskStatus.WaitingForActivation
            or TaskStatus.WaitingToRun
            or TaskStatus.Running
            or TaskStatus.WaitingForChildrenToComplete;

    /// <summary>Determines whether a task has been created but not yet started.</summary>
    /// <param name="task">The task to inspect.</param>
    /// <returns><c>true</c> if the task status is <see cref="System.Threading.Tasks.TaskStatus.Created" />.</returns>
    public static bool IsNotStarted(this Task task) => task?.Status == TaskStatus.Created;

    /// <summary>Determines whether a task has run to completion, been canceled or faulted.</summary>
    /// <param name="task">The task to inspect.</param>
    /// <returns><c>true</c> if the task has finished; <c>false</c> when it has not or is null.</returns>
    public static bool IsFinished(this Task task) =>
        task?.Status is TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted;

    /// <summary>Determines whether a task was canceled or faulted.</summary>
    /// <param name="task">The task to inspect.</param>
    /// <returns><c>true</c> if the task was canceled or faulted.</returns>
    public static bool IsFailed(this Task task) => task?.Status is TaskStatus.Canceled or TaskStatus.Faulted;

    /// <summary>
    /// Creates a task that will complete when all the supplied tasks have completed.
    /// </summary>
    /// <param name="tasks">The tasks to wait on for completion.</param>
    /// <returns>A task that represents the completion of all the supplied tasks.</returns>
    /// <remarks>
    /// <para>
    /// If any of the supplied tasks completes in a faulted state, the returned task will also complete in a Faulted state,
    /// where its exceptions will contain the aggregation of the set of unwrapped exceptions from each of the supplied tasks.
    /// </para>
    /// <para>
    /// If none of the supplied tasks faulted but at least one of them was canceled, the returned task will end in the Canceled
    /// state.
    /// </para>
    /// <para>
    /// If none of the tasks faulted and none of the tasks were canceled, the resulting task will end in the RanToCompletion
    /// state.
    /// </para>
    /// <para>
    /// If the supplied array/enumerable contains no tasks, the returned task will immediately transition to a RanToCompletion
    /// state before it's returned to the caller.
    /// </para>
    /// <para>
    /// It is intentionally immediately converted to array, as it is most efficient way due
    /// to internal implementation of <c>Task.WhenAll</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="tasks" /> argument was null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="tasks" /> array contained a null task.
    /// </exception>
    public static async Task WhenAllAsync(this IEnumerable<Task> tasks)
    {
        if (tasks is Task[] taskArray)
        {
            await Task.WhenAll(taskArray);

            return;
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Creates a task that will complete when all the supplied tasks have completed.
    /// </summary>
    /// <param name="tasks">The tasks to wait on for completion.</param>
    /// <returns>A task that represents the completion of all the supplied tasks.</returns>
    /// <remarks>
    /// <para>
    /// If any of the supplied tasks completes in a faulted state, the returned task will also complete in a Faulted state,
    /// where its exceptions will contain the aggregation of the set of unwrapped exceptions from each of the supplied tasks.
    /// </para>
    /// <para>
    /// If none of the supplied tasks faulted but at least one of them was canceled, the returned task will end in the Canceled
    /// state.
    /// </para>
    /// <para>
    /// If none of the tasks faulted and none of the tasks were canceled, the resulting task will end in the RanToCompletion
    /// state.
    /// The Result of the returned task will be set to an array containing all the results of the
    /// supplied tasks in the same order as they were provided (e.g. if the input tasks array contained t1, t2, t3, the output
    /// task's Result will return an TResult[] where arr[0] == t1.Result, arr[1] == t2.Result, and arr[2] == t3.Result).
    /// </para>
    /// <para>
    /// If the supplied array/enumerable contains no tasks, the returned task will immediately transition to a RanToCompletion
    /// state before it's returned to the caller.  The returned TResult[] will be an array of 0 elements.
    /// </para>
    /// <para>
    /// It is intentionally immediately converted to array, as it is most efficient way due
    /// to internal implementation of <c>Task.WhenAll</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="tasks" /> argument was null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="tasks" /> array contained a null task.
    /// </exception>
    public static async Task<T[]> WhenAllAsync<T>(this IEnumerable<Task<T>> tasks) =>
        tasks is Task<T>[] taskArray
            ? await Task.WhenAll(taskArray)
            : await Task.WhenAll(tasks.ToArray());

    /// <summary>Invokes an action and returns null so it can be used where a result-producing delegate is expected.</summary>
    /// <param name="action">The action to invoke.</param>
    /// <returns>Always null.</returns>
    public static object? Wrap(this Action action)
    {
        action();

        return null;
    }

    /// <summary>Adapts a task factory to one that yields a null result.</summary>
    /// <param name="taskFunc">The task factory to wrap.</param>
    /// <returns>A function that awaits the task and returns null.</returns>
    public static Func<Task<object?>> WrapTask(this Func<Task> taskFunc) => taskFunc.WrapTaskAsync;

    /// <summary>Adapts a one-argument task factory to a parameterless one that yields a null result.</summary>
    /// <typeparam name="T">The argument type.</typeparam>
    /// <param name="taskFunc">The task factory to wrap.</param>
    /// <param name="arg">The argument passed to the factory.</param>
    /// <returns>A function that awaits the task and returns null.</returns>
    public static Func<Task<object?>> WrapTask<T>(this Func<T, Task> taskFunc, T arg) =>
        () => taskFunc.WrapTaskAsync(arg);

    /// <summary>Invokes a task factory with an argument, awaits it and returns null.</summary>
    /// <typeparam name="T">The argument type.</typeparam>
    /// <param name="taskFunc">The task factory.</param>
    /// <param name="arg">The argument passed to the factory.</param>
    /// <returns>A task that yields null.</returns>
    public static async Task<object?> WrapTaskAsync<T>(this Func<T, Task> taskFunc, T arg)
    {
        await taskFunc(arg);

        return null;
    }

    /// <summary>Invokes a task factory, awaits it and returns null.</summary>
    /// <param name="taskFunc">The task factory.</param>
    /// <returns>A task that yields null.</returns>
    public static async Task<object?> WrapTaskAsync(this Func<Task> taskFunc)
    {
        await taskFunc();

        return null;
    }

    private static Task TaskSelectorAsync(Action action,
                                          AsyncMode mode,
                                          AsyncOptions options,
                                          CancellationToken cancellationToken) =>
        mode switch
        {
            AsyncMode.Default => Task.Run(action, cancellationToken),
            AsyncMode.MainThread => options.HasFlagFast(AsyncOptions.ImmediateStart)
                ? ExecuteDeferredTaskOnMainThreadAsync(() => Task.Run(action, cancellationToken))
                : ExecuteDeferredTaskOnMainThreadAsync(action),
            AsyncMode.ThreadPool => AsyncStatics.ExecuteOnThreadPoolAsync(action, options, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Mode {mode} is not supported")
        };

    private static Task<T> TaskSelectorAsync<T>(Func<T> func,
                                                AsyncMode mode,
                                                AsyncOptions options,
                                                CancellationToken cancellationToken) =>
        mode switch
        {
            AsyncMode.Default => Task.Run(func, cancellationToken),
            AsyncMode.MainThread => options.HasFlagFast(AsyncOptions.ImmediateStart)
                ? ExecuteDeferredTaskOnMainThreadAsync(() => Task.Run(func, cancellationToken))
                : ExecuteDeferredTaskOnMainThreadAsync(func),
            AsyncMode.ThreadPool => AsyncStatics.ExecuteOnThreadPoolAsync(func, options, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Mode {mode} is not supported")
        };

    private static Task TaskSelectorAsync(Func<Task> task, AsyncMode mode, AsyncOptions options) =>
        mode switch
        {
            AsyncMode.Default => Task.Run(task),
            AsyncMode.MainThread => options.HasFlagFast(AsyncOptions.ImmediateStart)
                ? ExecuteDeferredTaskOnMainThreadAsync(() => Task.Run(task))
                : ExecuteDeferredTaskOnMainThreadAsync(task),
            AsyncMode.ThreadPool => AsyncStatics.ExecuteTaskOnThreadPoolAsync(task, options),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Mode {mode} is not supported")
        };

    private static Task<T> TaskSelectorAsync<T>(Func<Task<T>> task, AsyncMode mode, AsyncOptions options) =>
        mode switch
        {
            AsyncMode.Default => Task.Run(task),
            AsyncMode.MainThread => options.HasFlagFast(AsyncOptions.ImmediateStart)
                ? ExecuteDeferredTaskOnMainThreadAsync(() => Task.Run(task))
                : ExecuteDeferredTaskOnMainThreadAsync(task),
            AsyncMode.ThreadPool => AsyncStatics.ExecuteTaskOnThreadPoolAsync(task, options),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Mode {mode} is not supported")
        };

    private static async Task ExecuteDeferredTaskOnMainThreadAsync(Action action, IAsyncHelper? asyncHelper = null) =>
        await (asyncHelper ?? AsyncHelper).ExecuteDeferredTaskOnMainThreadAsync(action);

    private static async Task<T>
        ExecuteDeferredTaskOnMainThreadAsync<T>(Func<T> func, IAsyncHelper? asyncHelper = null) =>
        await (asyncHelper ?? AsyncHelper).ExecuteDeferredTaskOnMainThreadAsync(func);

    private static async Task ExecuteDeferredTaskOnMainThreadAsync(Func<Task> task, IAsyncHelper? asyncHelper = null) =>
        await (asyncHelper ?? AsyncHelper).ExecuteDeferredTaskOnMainThreadAsync(task);

    private static async Task<T>
        ExecuteDeferredTaskOnMainThreadAsync<T>(Func<Task<T>> task, IAsyncHelper? asyncHelper = null) =>
        await (asyncHelper ?? AsyncHelper).ExecuteDeferredTaskOnMainThreadAsync(task);
}