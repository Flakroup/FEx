using FEx.Asyncx.Helpers;
using FEx.Extensions;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Asyncx.Extensions;

public static class JoinableTaskExtensions
{
    public static void WaitWithoutThreadLock(this Func<Task> taskFactory)
    {
        JoinableAsyncHelper.AwaitWithoutDeadlock(taskFactory);
    }

    public static T WaitWithoutThreadLock<T>(this Func<Task<T>> taskFactory) =>
        JoinableAsyncHelper.AwaitWithoutDeadlock(taskFactory);

    public static T[] WaitWithoutThreadLock<T>(this Func<Task<T[]>> tasksToAwait) =>
        JoinableAsyncHelper.AwaitWithoutDeadlock(tasksToAwait);

    public static async Task WaitWithoutThreadLockAsync(this Func<Task> taskFactory)
    {
        await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(taskFactory);
    }

    public static async Task<T> WaitWithoutThreadLockAsync<T>(this Func<Task<T>> taskFactory) =>
        await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(taskFactory);

    public static async Task<T[]> WaitWithoutThreadLockAsync<T>(this Func<Task<T[]>> tasksToAwait) =>
        await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(tasksToAwait);

    public static async Task<T[]> WhenAllJoinedAsync<T>(this IEnumerable<JoinableTask<T>> tasks) =>
        await tasks.RunFuncTaskWithWhenAllAsync(AwaitJoinableTaskAsync);

    public static async Task WhenAllJoinedAsync(this IEnumerable<JoinableTask> tasks)
    {
        await tasks.RunFuncTaskWithWhenAllAsync(AwaitJoinableTaskAsync);
    }

    private static async Task<T> AwaitJoinableTaskAsync<T>(JoinableTask<T> arg) => await arg;

    private static async Task AwaitJoinableTaskAsync(JoinableTask arg)
    {
        await arg;
    }
}