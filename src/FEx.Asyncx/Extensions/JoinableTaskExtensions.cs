using FEx.Abstractions.Enums;
using FEx.Abstractions.Extensions;
using FEx.Asyncx.Helpers;
using FEx.Extensions;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Asyncx.Extensions;

public static class JoinableTaskExtensions
{
    public static void WaitWithoutThreadLock(this Func<Task> taskFactory,
                                             AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart) =>
        JoinableAsyncHelper.AwaitWithoutDeadlock(options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static T WaitWithoutThreadLock<T>(this Func<Task<T>> taskFactory,
                                             AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart) =>
        JoinableAsyncHelper.AwaitWithoutDeadlock(options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static T[] WaitWithoutThreadLock<T>(this Func<Task<T[]>> taskFactory,
                                               AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart) =>
        JoinableAsyncHelper.AwaitWithoutDeadlock(options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static async Task WaitWithoutThreadLockAsync(this Func<Task> taskFactory,
                                                        AsyncHelperOptions options =
                                                            AsyncHelperOptions.ImmediateStart) =>
        await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static async Task<T> WaitWithoutThreadLockAsync<T>(this Func<Task<T>> taskFactory,
                                                              AsyncHelperOptions options =
                                                                  AsyncHelperOptions.ImmediateStart) =>
        await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static async Task<T[]> WaitWithoutThreadLockAsync<T>(this Func<Task<T[]>> taskFactory,
                                                                AsyncHelperOptions options =
                                                                    AsyncHelperOptions.ImmediateStart) =>
        await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
            ? () => Task.Run(taskFactory)
            : taskFactory);

    public static async Task<T[]> WhenAllJoinedAsync<T>(this IEnumerable<JoinableTask<T>> tasks) =>
        await tasks.RunWithWhenAllTasksAsync(AwaitJoinableTaskAsync);

    public static async Task WhenAllJoinedAsync(this IEnumerable<JoinableTask> tasks) =>
        await tasks.RunWithWhenAllTasksAsync(AwaitJoinableTaskAsync);

    private static async Task<T> AwaitJoinableTaskAsync<T>(JoinableTask<T> arg) => await arg;

    private static async Task AwaitJoinableTaskAsync(JoinableTask arg) => await arg;
}