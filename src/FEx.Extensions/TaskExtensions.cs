using FEx.Abstractions;
using FEx.Abstractions.Enums;
using FEx.Abstractions.Extensions;
using FEx.Extensions.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Extensions;

public static class TaskExtensions
{
    /// <summary>
    ///     Waits for task to start.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <returns>
    ///     Task
    /// </returns>
    public static async Task WaitForTaskToStartAsync(this Task task) =>
        await StaticAsyncHelper.DelayUntilAsync(task.IsNotStarted, milliseconds: 1);

    public static async Task WaitForTaskToEndAsync(this Task task) =>
        await StaticAsyncHelper.DelayUntilAsync(() => !task.IsFinished(), milliseconds: 1);

    public static bool IsRunning(this Task task) =>
        task?.Status is TaskStatus.WaitingForActivation
            or TaskStatus.WaitingToRun
            or TaskStatus.Running
            or TaskStatus.WaitingForChildrenToComplete;

    public static bool IsNotStarted(this Task task) => task?.Status == TaskStatus.Created;

    public static bool IsFinished(this Task task) =>
        task?.Status is TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted;

    public static bool IsFailed(this Task task) => task?.Status is TaskStatus.Canceled or TaskStatus.Faulted;

    public static async Task RunWhenAllAsync(this IEnumerable<Action> tasks,
                                             AsyncMode mode = AsyncMode.Default,
                                             AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart,
                                             CancellationToken cancellationToken = default) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options, cancellationToken)).WhenAllAsync();

    public static async Task<T[]> RunWhenAllAsync<T>(this IEnumerable<Func<T>> tasks,
                                                     AsyncMode mode = AsyncMode.Default,
                                                     AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart,
                                                     CancellationToken cancellationToken = default) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options, cancellationToken)).WhenAllAsync();

    public static async Task RunWhenAllTasksAsync(this IEnumerable<Func<Task>> tasks,
                                                  AsyncMode mode = AsyncMode.Default,
                                                  AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options)).WhenAllAsync();

    public static async Task<T[]> RunWhenAllTasksAsync<T>(this IEnumerable<Func<Task<T>>> tasks,
                                                          AsyncMode mode = AsyncMode.Default,
                                                          AsyncHelperOptions options =
                                                              AsyncHelperOptions.ImmediateStart) =>
        await tasks.Select(action => TaskSelectorAsync(action, mode, options)).WhenAllAsync();

    public static async Task RunWithWhenAllAsync<T>(this IEnumerable<T> values,
                                                    Action<T> asyncAction,
                                                    AsyncMode mode = AsyncMode.Default,
                                                    AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart,
                                                    CancellationToken cancellationToken = default)
    {
        if (options.HasFlagFast(AsyncHelperOptions.ImmediateStart))
        {
            await values.Select<T, Func<Task>>(v => () => Task.Run(() => asyncAction(v), cancellationToken))
                .RunWhenAllTasksAsync(mode);

            return;
        }

        await values.Select<T, Action>(v => () => asyncAction(v)).RunWhenAllAsync(mode, options, cancellationToken);
    }

    public static async Task<TResult[]> RunWithWhenAllAsync<T, TResult>(this IEnumerable<T> values,
                                                                        Func<T, TResult> asyncAction,
                                                                        AsyncMode mode = AsyncMode.Default,
                                                                        AsyncHelperOptions options =
                                                                            AsyncHelperOptions.ImmediateStart,
                                                                        CancellationToken cancellationToken = default)
    {
        if (options.HasFlagFast(AsyncHelperOptions.ImmediateStart))
            return await values
                .Select<T, Func<Task<TResult>>>(v => () => Task.Run(() => asyncAction(v), cancellationToken))
                .RunWhenAllTasksAsync(mode);

        return await values.Select<T, Func<TResult>>(v => () => asyncAction(v))
            .RunWhenAllAsync(mode, options, cancellationToken);
    }

    public static async Task RunWithWhenAllTasksAsync<T>(this IEnumerable<T> values,
                                                         Func<T, Task> asyncAction,
                                                         AsyncMode mode = AsyncMode.Default,
                                                         AsyncHelperOptions options =
                                                             AsyncHelperOptions.ImmediateStart) =>
        await values.Select<T, Func<Task>>(v => () => asyncAction(v)).RunWhenAllTasksAsync(mode, options);

    public static async Task<TResult[]> RunWithWhenAllTasksAsync<T, TResult>(this IEnumerable<T> values,
                                                                             Func<T, Task<TResult>> asyncAction,
                                                                             AsyncMode mode = AsyncMode.Default,
                                                                             AsyncHelperOptions options =
                                                                                 AsyncHelperOptions.ImmediateStart) =>
        await values.Select<T, Func<Task<TResult>>>(v => () => asyncAction(v)).RunWhenAllTasksAsync(mode, options);

    public static Func<Task<object>> WrapTask(this Func<Task> taskFunc) => () => WrapTaskAsync(taskFunc);

    public static Func<Task<object>> WrapTask<T>(this Func<T, Task> taskFunc, T arg) =>
        () => WrapTaskAsync(taskFunc, arg);

    /// <summary>
    /// Creates a task that will complete when all of the supplied tasks have completed.
    /// </summary>
    /// <param name="tasks">The tasks to wait on for completion.</param>
    /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
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
    /// It is intentionally immediatly converted to array, as it is most efficient way due
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
    /// Creates a task that will complete when all of the supplied tasks have completed.
    /// </summary>
    /// <param name="tasks">The tasks to wait on for completion.</param>
    /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
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
    /// The Result of the returned task will be set to an array containing all of the results of the
    /// supplied tasks in the same order as they were provided (e.g. if the input tasks array contained t1, t2, t3, the output
    /// task's Result will return an TResult[] where arr[0] == t1.Result, arr[1] == t2.Result, and arr[2] == t3.Result).
    /// </para>
    /// <para>
    /// If the supplied array/enumerable contains no tasks, the returned task will immediately transition to a RanToCompletion
    /// state before it's returned to the caller.  The returned TResult[] will be an array of 0 elements.
    /// </para>
    /// <para>
    /// It is intentionally immediatly converted to array, as it is most efficient way due
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

    private static Task TaskSelectorAsync(Action action,
                                          AsyncMode mode,
                                          AsyncHelperOptions options,
                                          CancellationToken cancellationToken) =>
        mode switch
        {
            AsyncMode.Default => Task.Run(action, cancellationToken),
            AsyncMode.MainThread => FExFoundation.AsyncHelper.ExecuteDeferredTaskOnMainThreadAsync(() =>
                options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
                    ? Task.Run(action, cancellationToken)
                    : Wrap(action)),
            AsyncMode.ThreadPool => StaticAsyncHelper.ExecuteOnThreadPoolAsync(action, options, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Mode {mode} is not supported")
        };

    private static Task<T> TaskSelectorAsync<T>(Func<T> func,
                                                AsyncMode mode,
                                                AsyncHelperOptions options,
                                                CancellationToken cancellationToken) =>
        mode switch
        {
            AsyncMode.Default => Task.Run(func, cancellationToken),
            AsyncMode.MainThread => options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
                ? FExFoundation.AsyncHelper.ExecuteDeferredTaskOnMainThreadAsync(
                    () => Task.Run(func, cancellationToken))
                : FExFoundation.AsyncHelper.ExecuteDeferredTaskOnMainThreadAsync(func),
            AsyncMode.ThreadPool => StaticAsyncHelper.ExecuteOnThreadPoolAsync(func, options, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Mode {mode} is not supported")
        };

    private static Task TaskSelectorAsync(Func<Task> task, AsyncMode mode, AsyncHelperOptions options) =>
        mode switch
        {
            AsyncMode.Default => Task.Run(task),
            AsyncMode.MainThread => options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
                ? FExFoundation.AsyncHelper.ExecuteDeferredTaskOnMainThreadAsync(() => Task.Run(task))
                : FExFoundation.AsyncHelper.ExecuteDeferredTaskOnMainThreadAsync(task),
            AsyncMode.ThreadPool => StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(task, options),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Mode {mode} is not supported")
        };

    private static Task<T> TaskSelectorAsync<T>(Func<Task<T>> task, AsyncMode mode, AsyncHelperOptions options) =>
        mode switch
        {
            AsyncMode.Default => Task.Run(task),
            AsyncMode.MainThread => options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
                ? FExFoundation.AsyncHelper.ExecuteDeferredTaskOnMainThreadAsync(() => Task.Run(task))
                : FExFoundation.AsyncHelper.ExecuteDeferredTaskOnMainThreadAsync(task),
            AsyncMode.ThreadPool => StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(task, options),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Mode {mode} is not supported")
        };

    private static object Wrap(Action action)
    {
        action();

        return null;
    }

    private static async Task<object> WrapTaskAsync<T>(Func<T, Task> taskFunc, T arg)
    {
        await taskFunc(arg);

        return null;
    }

    private static async Task<object> WrapTaskAsync(Func<Task> taskFunc)
    {
        await taskFunc();

        return null;
    }
}