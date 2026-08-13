using FEx.Agnostics.Abstractions.Enums;
using FEx.Legacy.Asyncx.Abstractions.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Legacy.Asyncx.Extensions;

public static class TasksHandlerExtensions
{
    public static Task RunAsync(this ITasksHandler handler, Action task) =>
        handler.RunAsync(task, null, null, null, AsyncMode.ThreadPool, CancellationToken.None);

    public static Task<T> RunTaskAsync<T>(this ITasksHandler handler, Func<Task<T>> task) =>
        handler.RunTaskAsync(task, null, null, null, AsyncMode.ThreadPool);

    public static Task RunTaskAsync(this ITasksHandler handler, Func<Task> task) =>
        handler.RunTaskAsync(task, null, null, null, AsyncMode.ThreadPool);

    public static Task<T> RunFuncAsync<T>(this ITasksHandler handler, Func<T> task) =>
        handler.RunFuncAsync(task, null, null, null, AsyncMode.ThreadPool, CancellationToken.None);
}