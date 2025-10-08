using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Helpers;
using FEx.Core.Abstractions;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Asyncx.Extensions;

public static class JoinableTaskExtensions
{
    public static void WaitWithoutThreadLock(this Func<Task> taskFactory,
                                             AsyncOptions options = AsyncOptions.ImmediateStart) =>
        JoinableAsyncHelper.AwaitWithoutDeadlock(options.HasFlagFast(AsyncOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static T WaitWithoutThreadLock<T>(this Func<Task<T>> taskFactory,
                                             AsyncOptions options = AsyncOptions.ImmediateStart) =>
        JoinableAsyncHelper.AwaitWithoutDeadlock(options.HasFlagFast(AsyncOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static T[] WaitWithoutThreadLock<T>(this Func<Task<T[]>> taskFactory,
                                               AsyncOptions options = AsyncOptions.ImmediateStart) =>
        JoinableAsyncHelper.AwaitWithoutDeadlock(options.HasFlagFast(AsyncOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static async Task WaitWithoutThreadLockAsync(this Func<Task> taskFactory,
                                                        AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(options.HasFlagFast(AsyncOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static async Task<T> WaitWithoutThreadLockAsync<T>(this Func<Task<T>> taskFactory,
                                                              AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(options.HasFlagFast(AsyncOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static async Task<T[]> WaitWithoutThreadLockAsync<T>(this Func<Task<T[]>> taskFactory,
                                                                AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(options.HasFlagFast(AsyncOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static async Task<T[]> WhenAllJoinedAsync<T>(this IEnumerable<JoinableTask<T>> tasks) =>
        await tasks.WithWhenAllTasksAsync(AwaitJoinableTaskAsync);

    public static async Task WhenAllJoinedAsync(this IEnumerable<JoinableTask> tasks) =>
        await tasks.WithWhenAllTasksAsync(AwaitJoinableTaskAsync);

    public static void FireOrWait(Func<Task> func, bool wait)
    {
        if (wait)
            JoinableAsyncHelper.AwaitWithoutDeadlock(func);
        else
            FExCoreStatics.AsyncHelper.FireTaskAndForget(func);
    }

    public static T FireOrWait<T>(Func<Task<T>> func, bool wait)
    {
        if (wait)
            return JoinableAsyncHelper.AwaitWithoutDeadlock(func);

        FExCoreStatics.AsyncHelper.FireTaskAndForget(func);

        return default;
    }

    public static async Task SwitchToThreadPoolAsync(Func<Task> function)
    {
        await TaskScheduler.Default;
        await function();
    }

    public static async Task<T> SwitchToThreadPoolAsync<T>(Func<Task<T>> function)
    {
        await TaskScheduler.Default;

        return await function();
    }

    private static async Task<T> AwaitJoinableTaskAsync<T>(JoinableTask<T> arg) => await arg;

    private static async Task AwaitJoinableTaskAsync(JoinableTask arg) => await arg;
}