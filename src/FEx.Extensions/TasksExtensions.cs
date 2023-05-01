using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Extensions;

public static class TasksExtensions
{
    public static bool IsRunning(this Task task)
    {
        return task is not null && (task.Status == TaskStatus.WaitingForActivation || task.Status == TaskStatus.WaitingToRun || task.Status == TaskStatus.Running || task.Status == TaskStatus.WaitingForChildrenToComplete);
    }

    public static bool IsNotStarted(this Task task)
    {
        return task?.Status == TaskStatus.Created;
    }

    public static bool IsFinished(this Task task)
    {
        return task is not null && (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled || task.Status == TaskStatus.Faulted);
    }

    public static bool IsFailed(this Task task)
    {
        return task is not null && (task.Status == TaskStatus.Canceled || task.Status == TaskStatus.Faulted);
    }

    public static async Task WhenAllAsync(this IEnumerable<Task> tasksToBeStarted)
    {
        await Task.WhenAll(tasksToBeStarted);
    }

    public static async Task<T[]> WhenAllAsync<T>(this IEnumerable<Task<T>> tasksToBeStarted)
    {
        return await Task.WhenAll(tasksToBeStarted);
    }

    public static async Task WhenAllAsync<T>(this IEnumerable<T> values, Func<T, Task> asyncAction)
    {
        await Task.WhenAll(values.Select(asyncAction));
    }

    public static async Task<TRes[]> WhenAllAsync<T, TRes>(this IEnumerable<T> values, Func<T, Task<TRes>> asyncAction)
    {
        return await Task.WhenAll(values.Select(asyncAction));
    }

    public static async Task<T[]> RunFuncWithWhenAllAsync<T>(this IEnumerable<Func<T>> tasksToBeStarted, bool immediateStart = false)
    {
        return await WhenAllAsync(immediateStart, tasksToBeStarted.Select(Task.Run));
    }

    public static async Task RunFuncTaskWithWhenAllAsync(this IEnumerable<Func<Task>> tasksToBeStarted, bool immediateStart = false)
    {
        await WhenAllAsync(immediateStart, tasksToBeStarted.Select(Task.Run));
    }

    public static async Task<T[]> RunFuncTaskWithWhenAllAsync<T>(this IEnumerable<Func<Task<T>>> tasksToBeStarted, bool immediateStart = false)
    {
        return await WhenAllAsync(immediateStart, tasksToBeStarted.Select(Task.Run));
    }

    public static async Task RunFuncTaskWithWhenAllAsync(this IEnumerable<Func<Task>> tasksToBeStarted, CancellationToken cancellationToken, bool immediateStart = false)
    {
        await WhenAllAsync(immediateStart, tasksToBeStarted.Select(x => Task.Run(x, cancellationToken)));
    }

    public static async Task<T[]> RunFuncTaskWithWhenAllAsync<T>(this IEnumerable<Func<Task<T>>> tasksToBeStarted, CancellationToken cancellationToken, bool immediateStart = false)
    {
        return await WhenAllAsync(immediateStart, tasksToBeStarted.Select(x => Task.Run(x, cancellationToken)));
    }

    public static async Task RunWithWhenAllAsync(this IEnumerable<Action> tasksToBeStarted, bool immediateStart = false)
    {
        await WhenAllAsync(immediateStart, tasksToBeStarted.Select(Task.Run));
    }

    public static async Task RunWithWhenAllAsync(this IEnumerable<Action> tasksToBeStarted, CancellationToken cancellationToken, bool immediateStart = false)
    {
        await WhenAllAsync(immediateStart, tasksToBeStarted.Select(x => Task.Run(x, cancellationToken)));
    }

    public static async Task RunWithWhenAllAsync<T>(this IEnumerable<T> values, Action<T> asyncAction, bool immediateStart = false)
    {
        await WhenAllAsync(immediateStart, values.Select(x => Task.Run(() => asyncAction(x))));
    }

    public static async Task RunFuncWithWhenAllAsync<T>(this IEnumerable<T> values, Func<T, Action> asyncAction, bool immediateStart = false)
    {
        await WhenAllAsync(immediateStart, values.Select(x => Task.Run(() => asyncAction(x))));
    }

    public static async Task<TRet[]> RunFuncWithWhenAllAsync<T, TRet>(this IEnumerable<T> values, Func<T, TRet> asyncAction, bool immediateStart = false)
    {
        return await WhenAllAsync(immediateStart, values.Select(x => Task.Run(() => asyncAction(x))));
    }

    public static async Task RunFuncTaskWithWhenAllAsync<T>(this IEnumerable<T> values, Func<T, Task> asyncAction, bool immediateStart = false)
    {
        await WhenAllAsync(immediateStart, values.Select(x => Task.Run(() => asyncAction(x))));
    }

    public static async Task<TRes[]> RunFuncTaskWithWhenAllAsync<T, TRes>(this IEnumerable<T> values, Func<T, Task<TRes>> asyncAction, bool immediateStart = false)
    {
        return await WhenAllAsync(immediateStart, values.Select(x => Task.Run(() => asyncAction(x))));
    }

    public static async Task RunFuncTaskWithWhenAllAsync<T>(this IEnumerable<T> values, Func<T, Task> asyncAction, CancellationToken cancellationToken, bool immediateStart = false)
    {
        await WhenAllAsync(immediateStart, values.Select(x => Task.Run(() => asyncAction(x), cancellationToken)));
    }

    public static async Task<TRes[]> RunFuncTaskWithWhenAllAsync<T, TRes>(this IEnumerable<T> values, Func<T, Task<TRes>> asyncAction, CancellationToken cancellationToken, bool immediateStart = false)
    {
        return await WhenAllAsync(immediateStart, values.Select(x => Task.Run(() => asyncAction(x), cancellationToken)));
    }

    public static Func<Task<object>> WrapTask(this Func<Task> taskFunc)
    {
        return () => WrapTaskAsync(taskFunc);
    }

    public static Func<Task<object>> WrapTask<T>(this Func<T, Task> taskFunc, T arg)
    {
        return () => WrapTaskAsync(taskFunc, arg);
    }

    private static async Task WhenAllAsync(bool immediateStart, IEnumerable<Task> tasks)
    {
        await (immediateStart
            ? Task.WhenAll(Start(tasks))
            : Task.WhenAll(tasks));
    }

    private static async Task<TRes[]> WhenAllAsync<TRes>(bool immediateStart, IEnumerable<Task<TRes>> tasks)
    {
        return await (immediateStart
            ? Task.WhenAll(Start(tasks))
            : Task.WhenAll(tasks));
    }

    private static T[] Start<T>(IEnumerable<T> tasks)
    {
        return tasks.ToArray();
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