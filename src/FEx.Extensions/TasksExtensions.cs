using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Extensions;

public static class TasksExtensions
{
    public static bool IsRunning(this Task task) => task?.Status is TaskStatus.WaitingForActivation
        or TaskStatus.WaitingToRun
        or TaskStatus.Running
        or TaskStatus.WaitingForChildrenToComplete;

    public static bool IsNotStarted(this Task task) => task?.Status == TaskStatus.Created;

    public static bool IsFinished(this Task task) => task?.Status is TaskStatus.RanToCompletion
        or TaskStatus.Canceled
        or TaskStatus.Faulted;

    public static bool IsFailed(this Task task) =>
        task?.Status is TaskStatus.Canceled or TaskStatus.Faulted;

    public static async Task RunWithWhenAllAsync(this IEnumerable<Action> tasksToBeStarted,
                                                 bool immediateStart = true,
                                                 CancellationToken cancellationToken = default) =>
        await WhenAllAsync(immediateStart, tasksToBeStarted.Select(x => Task.Run(x, cancellationToken)));

    public static async Task<T[]> RunFuncWithWhenAllAsync<T>(this IEnumerable<Func<T>> tasksToBeStarted,
                                                             bool immediateStart = true,
                                                             CancellationToken cancellationToken = default) =>
        await WhenAllAsync(immediateStart, tasksToBeStarted.Select(x => Task.Run(x, cancellationToken)));

    public static async Task RunFuncTaskWithWhenAllAsync(this IEnumerable<Func<Task>> tasksToBeStarted,
                                                         bool immediateStart = true) =>
        await WhenAllAsync(immediateStart, tasksToBeStarted.Select(x => x()));

    public static async Task<T[]> RunFuncTaskWithWhenAllAsync<T>(this IEnumerable<Func<Task<T>>> tasksToBeStarted,
                                                                 bool immediateStart = true) =>
        await WhenAllAsync(immediateStart, tasksToBeStarted.Select(x => x()));

    public static async Task RunWithWhenAllAsync<T>(this IEnumerable<T> values,
                                                    Action<T> asyncAction,
                                                    bool immediateStart = true,
                                                    CancellationToken cancellationToken = default) =>
        await WhenAllAsync(immediateStart, values.Select(x => Task.Run(() => asyncAction(x), cancellationToken)));

    public static async Task<TRet[]> RunFuncWithWhenAllAsync<T, TRet>(this IEnumerable<T> values,
                                                                      Func<T, TRet> asyncAction,
                                                                      bool immediateStart = true,
                                                                      CancellationToken cancellationToken = default) =>
        await WhenAllAsync(immediateStart, values.Select(x => Task.Run(() => asyncAction(x), cancellationToken)));

    public static async Task RunFuncTaskWithWhenAllAsync<T>(this IEnumerable<T> values,
                                                            Func<T, Task> asyncAction,
                                                            bool immediateStart = true) =>
        await WhenAllAsync(immediateStart, values.Select(asyncAction));

    public static async Task<TRes[]> RunFuncTaskWithWhenAllAsync<T, TRes>(
        this IEnumerable<T> values,
        Func<T, Task<TRes>> asyncAction,
        bool immediateStart = true) =>
        await WhenAllAsync(immediateStart, values.Select(asyncAction));

    public static async Task WhenAllAsync(this IEnumerable<Task> tasksToBeStarted, bool immediateStart = true) =>
        await WhenAllAsync(immediateStart, tasksToBeStarted);

    public static async Task<T[]> WhenAllAsync<T>(this IEnumerable<Task<T>> tasksToBeStarted,
                                                  bool immediateStart = true) =>
        await WhenAllAsync(immediateStart, tasksToBeStarted);

    public static async Task WhenAllAsync<T>(this IEnumerable<T> values,
                                             Func<T, Task> asyncAction,
                                             bool immediateStart = true) =>
        await WhenAllAsync(immediateStart, values.Select(asyncAction));

    public static async Task<TRes[]> WhenAllAsync<T, TRes>(this IEnumerable<T> values,
                                                           Func<T, Task<TRes>> asyncAction,
                                                           bool immediateStart = true) =>
        await WhenAllAsync(immediateStart, values.Select(asyncAction));

    public static Func<Task<object>> WrapTask(this Func<Task> taskFunc) => () => WrapTaskAsync(taskFunc);

    public static Func<Task<object>> WrapTask<T>(this Func<T, Task> taskFunc, T arg) =>
        () => WrapTaskAsync(taskFunc, arg);

    private static async Task WhenAllAsync(bool immediateStart, IEnumerable<Task> tasks) =>
        await (immediateStart
            ? Task.WhenAll(Start(tasks))
            : Task.WhenAll(tasks));

    private static async Task<TRes[]> WhenAllAsync<TRes>(bool immediateStart, IEnumerable<Task<TRes>> tasks) =>
        await (immediateStart
            ? Task.WhenAll(Start(tasks))
            : Task.WhenAll(tasks));

    private static List<T> Start<T>(IEnumerable<T> tasks) => tasks.ToList();

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