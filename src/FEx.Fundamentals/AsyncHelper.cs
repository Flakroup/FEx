using FEx.Abstractions;
using FEx.Extensions;
using FEx.Logging.Abstractions;
using GuardNet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Fundamentals;

public class AsyncHelper
{
    private readonly IFExDispatcher _dispatcher;
    private readonly ILogger _logger;

    public AsyncHelper(IFExDispatcher dispatcher, ILogger<AsyncHelper> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    private static object Wrap(Action action)
    {
        action();
        return null;
    }

    private static async Task<object> WrapTask(Func<Task> task)
    {
        await task();
        return null;
    }

    public TaskCompletionSource<object> FireAndForget(Action action,
                                                      CancellationToken cancellationToken = default,
                                                      AsyncMode asyncMode = AsyncMode.Default)
    {
        action.Guard(nameof(action));
        return FireAndForget(() => Wrap(action), cancellationToken, asyncMode);
    }

    public TaskCompletionSource<T> FireAndForget<T>(Func<T> func,
                                                    CancellationToken cancellationToken = default,
                                                    AsyncMode asyncMode = AsyncMode.Default)
    {
        func.Guard(nameof(func));
        var taskCompletionSource = new TaskCompletionSource<T>();
        _ = ExecuteAndCatchAsync(func, taskCompletionSource, cancellationToken, asyncMode);
        return taskCompletionSource;
    }

    public TaskCompletionSource<object> FireTaskAndForget(Func<Task> task, AsyncMode asyncMode = AsyncMode.Default)
    {
        task.Guard(nameof(task));
        return FireTaskAndForget(() => WrapTask(task), asyncMode);
    }

    public TaskCompletionSource<T> FireTaskAndForget<T>(Func<Task<T>> task, AsyncMode asyncMode = AsyncMode.Default)
    {
        task.Guard(nameof(task));
        var taskCompletionSource = new TaskCompletionSource<T>();
        _ = ExecuteTaskAndCatchAsync(task, taskCompletionSource, asyncMode);
        return taskCompletionSource;
    }

    public IReadOnlyList<TaskCompletionSource<object>> FireTasksAndForget(
        IEnumerable<Func<Task>> tasks,
        AsyncMode asyncMode = AsyncMode.Default)
    {
        Guard.For(() => tasks?.Any() != true,
            new ArgumentNullException(nameof(tasks), $"The {nameof(tasks)} cannot be null or empty."));

        return tasks.Select(x => FireTaskAndForget(x, asyncMode)).ToList().AsReadOnly();
    }

    public IReadOnlyList<TaskCompletionSource<T>> FireTasksAndForget<T>(IEnumerable<Func<Task<T>>> tasks,
                                                                        AsyncMode asyncMode = AsyncMode.Default)
    {
        tasks.Guard(nameof(tasks));
        return tasks.Select(x => FireTaskAndForget(x, asyncMode)).ToList().AsReadOnly();
    }

    public async Task ExecuteTaskOnThreadPoolAsync(Func<Task> task)
    {
        await ExecuteTaskOnThreadPoolAsync(() => WrapTask(task));
    }

    public async Task<T> ExecuteTaskOnThreadPoolAsync<T>(Func<Task<T>> taskFunc, bool logException = true)
    {
        Task<T> task = await ExecuteOnThreadPoolAsync(taskFunc);

        try
        {
            return await task;
        }
        catch (Exception ex) when (logException)
        {
            _logger.LogError(ex);
            throw;
        }
    }

    public async Task<T> ExecuteOnThreadPoolAsync<T>(Func<T> func,
                                                     CancellationToken cancellationToken = default,
                                                     bool logException = true)
    {
        if (Thread.CurrentThread.IsThreadPoolThread)
            return func();

        try
        {
            return await new TaskFactory(TaskScheduler.Default).StartNew(func, cancellationToken);
        }
        catch (Exception ex) when (logException)
        {
            _logger.LogError(ex);
            throw;
        }
    }

    /// <summary>Creates a cancellable task that completes after a time delay.</summary>
    /// <param name="millisecondsDelay">
    ///     The number of milliseconds to wait before completing the returned task, or -1 to wait
    ///     indefinitely.
    /// </param>
    /// <param name="cancellationToken">The cancellation token that will be checked prior to completing the returned task.</param>
    /// <returns>A task that represents the time delay.</returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     The
    ///     <paramref name="millisecondsDelay">millisecondsDelay</paramref> argument is less than -1.
    /// </exception>
    /// <exception cref="T:System.Threading.Tasks.TaskCanceledException">The task has been canceled.</exception>
    /// <exception cref="T:System.ObjectDisposedException">
    ///     The provided
    ///     <paramref name="cancellationToken">cancellationToken</paramref> has already been disposed.
    /// </exception>
    public async Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken = default)
    {
        await ExecuteTaskOnThreadPoolAsync(() => Task.Delay(millisecondsDelay, cancellationToken));
    }

    /// <summary>Creates a cancellable task that completes after a specified time interval.</summary>
    /// <param name="delay">
    ///     The time span to wait before completing the returned task, or
    ///     <see langword="TimeSpan.FromMilliseconds(-1)" /> to wait indefinitely.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="delay" /> represents a negative time interval other than
    ///     <see langword="TimeSpan.FromMilliseconds(-1)" />.
    ///     -or-
    ///     The <paramref name="delay" /> argument's <see cref="P:System.TimeSpan.TotalMilliseconds" /> property is greater
    ///     than 4294967294 on .NET 6 and later versions, or <see cref="F:System.Int32.MaxValue">Int32.MaxValue</see> on all
    ///     previous versions.
    /// </exception>
    /// <exception cref="T:System.Threading.Tasks.TaskCanceledException">The task has been canceled.</exception>
    /// <exception cref="T:System.ObjectDisposedException">
    ///     The provided <paramref name="cancellationToken" /> has already been
    ///     disposed.
    /// </exception>
    /// <returns>A task that represents the time delay.</returns>
    public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        await ExecuteTaskOnThreadPoolAsync(() => Task.Delay(delay, cancellationToken));
    }

    private async Task ExecuteAndCatchAsync<T>(Func<T> func,
                                               TaskCompletionSource<T> taskCompletionSource,
                                               CancellationToken cancellationToken,
                                               AsyncMode asyncMode)
    {
        try
        {
            T result = asyncMode switch
            {
                AsyncMode.MainThread => await ExecuteTaskOnThreadPoolAsync(
                    () => _dispatcher.InvokeOnMainThreadAsync(func), false),
                AsyncMode.ThreadPool => await ExecuteOnThreadPoolAsync(func, cancellationToken, false),
                _ => func()
            };

            taskCompletionSource.SetResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex);
            taskCompletionSource.SetException(ex);
        }
    }

    private async Task ExecuteTaskAndCatchAsync<T>(Func<Task<T>> task,
                                                   TaskCompletionSource<T> taskCompletionSource,
                                                   AsyncMode asyncMode)
    {
        try
        {
            T result = asyncMode switch
            {
                AsyncMode.MainThread => await ExecuteTaskOnThreadPoolAsync(
                    () => _dispatcher.InvokeOnMainThreadAsync(task), false),
                AsyncMode.ThreadPool => await ExecuteTaskOnThreadPoolAsync(task, false),
                _ => await task()
            };

            taskCompletionSource.SetResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex);
            taskCompletionSource.SetException(ex);
        }
    }
}