using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class TaskExtensions
{
    private static IAsyncHelper AsyncHelper => FExAgnosticsStatics.AsyncHelper;

    public static async Task WhenAllAsync(this IEnumerable<Action> tasks,
                                          AsyncMode mode = AsyncMode.Default,
                                          AsyncOptions options = AsyncOptions.ImmediateStart,
                                          CancellationToken cancellationToken = default) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options, cancellationToken)).WhenAllAsync();

    public static async Task<T[]> WhenAllAsync<T>(this IEnumerable<Func<T>> tasks,
                                                  AsyncMode mode = AsyncMode.Default,
                                                  AsyncOptions options = AsyncOptions.ImmediateStart,
                                                  CancellationToken cancellationToken = default) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options, cancellationToken)).WhenAllAsync();

    public static async Task WhenAllTasksAsync(this IEnumerable<Func<Task>> tasks,
                                               AsyncMode mode = AsyncMode.Default,
                                               AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options)).WhenAllAsync();

    public static async Task<T[]> WhenAllTasksAsync<T>(this IEnumerable<Func<Task<T>>> tasks,
                                                       AsyncMode mode = AsyncMode.Default,
                                                       AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options)).WhenAllAsync();

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

    public static async Task WithWhenAllTasksAsync<T>(this IEnumerable<T> values,
                                                      Func<T, Task> asyncAction,
                                                      AsyncMode mode = AsyncMode.Default,
                                                      AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await values.Select<T, Func<Task>>(v => () => asyncAction(v)).WhenAllTasksAsync(mode, options);

    public static async Task<TResult[]> WithWhenAllTasksAsync<T, TResult>(this IEnumerable<T> values,
                                                                          Func<T, Task<TResult>> asyncAction,
                                                                          AsyncMode mode = AsyncMode.Default,
                                                                          AsyncOptions options =
                                                                              AsyncOptions.ImmediateStart) =>
        await values.Select<T, Func<Task<TResult>>>(v => () => asyncAction(v)).WhenAllTasksAsync(mode, options);

    public static ITaskWrapper FireOnMainThreadAndForget(this IAsyncHelper asyncHelper,
                                                         Action action,
                                                         CancellationToken cancellationToken = default) =>
        asyncHelper.FireAndForget(action, AsyncMode.MainThread, cancellationToken: cancellationToken);

    public static ITaskWrapper<T> FireOnMainThreadAndForget<T>(this IAsyncHelper asyncHelper,
                                                               Func<T> func,
                                                               CancellationToken cancellationToken = default) =>
        asyncHelper.FireAndForget(func, AsyncMode.MainThread, cancellationToken: cancellationToken);

    public static ITaskWrapper FireTaskOnMainThreadAndForget(this IAsyncHelper asyncHelper, Func<Task> task) =>
        asyncHelper.FireTaskAndForget(task, AsyncMode.MainThread);

    public static ITaskWrapper<T> FireTaskOnMainThreadAndForget<T>(this IAsyncHelper asyncHelper, Func<Task<T>> task) =>
        asyncHelper.FireTaskAndForget(task, AsyncMode.MainThread);

    public static IReadOnlyList<ITaskWrapper> FireTasksOnMainThreadAndForget(this IAsyncHelper asyncHelper,
                                                                             IEnumerable<Func<Task>> tasks) =>
        asyncHelper.FireTasksAndForget(tasks, AsyncMode.MainThread);

    public static IReadOnlyList<ITaskWrapper<T>> FireTasksOnMainThreadAndForget<T>(this IAsyncHelper asyncHelper,
        IEnumerable<Func<Task<T>>> tasks) =>
        asyncHelper.FireTasksAndForget(tasks, AsyncMode.MainThread);

    public static ITaskWrapper FireOnThreadPoolAndForget(this IAsyncHelper asyncHelper,
                                                         Action action,
                                                         CancellationToken cancellationToken = default) =>
        asyncHelper.FireAndForget(action, AsyncMode.ThreadPool, cancellationToken: cancellationToken);

    public static ITaskWrapper<T> FireOnThreadPoolAndForget<T>(this IAsyncHelper asyncHelper,
                                                               Func<T> func,
                                                               CancellationToken cancellationToken = default) =>
        asyncHelper.FireAndForget(func, AsyncMode.ThreadPool, cancellationToken: cancellationToken);

    public static ITaskWrapper FireTaskOnThreadPoolAndForget(this IAsyncHelper asyncHelper, Func<Task> task) =>
        asyncHelper.FireTaskAndForget(task, AsyncMode.ThreadPool);

    public static ITaskWrapper<T> FireTaskOnThreadPoolAndForget<T>(this IAsyncHelper asyncHelper, Func<Task<T>> task) =>
        asyncHelper.FireTaskAndForget(task, AsyncMode.ThreadPool);

    public static IReadOnlyList<ITaskWrapper> FireTasksOnThreadPoolAndForget(this IAsyncHelper asyncHelper,
                                                                             IEnumerable<Func<Task>> tasks) =>
        asyncHelper.FireTasksAndForget(tasks, AsyncMode.ThreadPool);

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

    public static async Task WaitForTaskToEndAsync(this Task task) =>
        await AsyncStatics.DelayUntilAsync(() => !task.IsFinished(), milliseconds: 1);

    public static bool IsRunning(this Task task) =>
        task?.Status is TaskStatus.WaitingForActivation
            or TaskStatus.WaitingToRun
            or TaskStatus.Running
            or TaskStatus.WaitingForChildrenToComplete;

    public static bool IsNotStarted(this Task task) => task?.Status == TaskStatus.Created;

    public static bool IsFinished(this Task task) =>
        task?.Status is TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted;

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

    public static object Wrap(this Action action)
    {
        action();

        return null;
    }

    public static Func<Task<object>> WrapTask(this Func<Task> taskFunc) => taskFunc.WrapTaskAsync;

    public static Func<Task<object>> WrapTask<T>(this Func<T, Task> taskFunc, T arg) =>
        () => taskFunc.WrapTaskAsync(arg);

    public static async Task<object> WrapTaskAsync<T>(this Func<T, Task> taskFunc, T arg)
    {
        await taskFunc(arg);

        return null;
    }

    public static async Task<object> WrapTaskAsync(this Func<Task> taskFunc)
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

    private static async Task ExecuteDeferredTaskOnMainThreadAsync(Action action, IAsyncHelper asyncHelper = null) =>
        await (asyncHelper ?? AsyncHelper).ExecuteDeferredTaskOnMainThreadAsync(action);

    private static async Task<T>
        ExecuteDeferredTaskOnMainThreadAsync<T>(Func<T> func, IAsyncHelper asyncHelper = null) =>
        await (asyncHelper ?? AsyncHelper).ExecuteDeferredTaskOnMainThreadAsync(func);

    private static async Task ExecuteDeferredTaskOnMainThreadAsync(Func<Task> task, IAsyncHelper asyncHelper = null) =>
        await (asyncHelper ?? AsyncHelper).ExecuteDeferredTaskOnMainThreadAsync(task);

    private static async Task<T>
        ExecuteDeferredTaskOnMainThreadAsync<T>(Func<Task<T>> task, IAsyncHelper asyncHelper = null) =>
        await (asyncHelper ?? AsyncHelper).ExecuteDeferredTaskOnMainThreadAsync(task);
}