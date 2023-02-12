using FEx.Abstractions;
using FEx.Extensions;
using FEx.Utilities.Enums;
using FEx.Utilities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Utilities.Helpers;

public class AsyncHelper
{
    private readonly IFExDispatcher _dispatcher;
    private readonly ILogger _logger;

    public AsyncHelper(IFExDispatcher dispatcher, ILogger logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    private static Func<Task<object>> WrapTask(Func<Task> task)
    {
        return async () =>
        {
            await task();
            return default;
        };
    }

    private static Func<object> Wrap(Action action)
    {
        return () =>
        {
            action();
            return default;
        };
    }

    public TaskCompletionSource<object> FireAndForget(Action action, CancellationToken cancellationToken, AsyncMode asyncMode = AsyncMode.Default, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        action.Guard(nameof(action));
        return FireAndForget(Wrap(action), cancellationToken, asyncMode);
    }

    public TaskCompletionSource<T> FireAndForget<T>(Func<T> func, CancellationToken cancellationToken, AsyncMode asyncMode = AsyncMode.Default, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        func.Guard(nameof(func));
        var taskCompletionSource = new TaskCompletionSource<T>();
        _ = ExecuteAndCatchAsync(func, taskCompletionSource, cancellationToken, asyncMode);
        return taskCompletionSource;
    }

    public TaskCompletionSource<object> FireTaskAndForget(Func<Task> task, AsyncMode asyncMode = AsyncMode.Default, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        task.Guard(nameof(task));
        return FireTaskAndForget(WrapTask(task), asyncMode);
    }

    public TaskCompletionSource<T> FireTaskAndForget<T>(Func<Task<T>> task, AsyncMode asyncMode = AsyncMode.Default, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        task.Guard(nameof(task));
        var taskCompletionSource = new TaskCompletionSource<T>();
        _ = ExecuteTaskAndCatchAsync(task, taskCompletionSource, asyncMode);
        return taskCompletionSource;
    }

    public IReadOnlyList<TaskCompletionSource<object>> FireTasksAndForget(IEnumerable<Func<Task>> tasks, AsyncMode asyncMode = AsyncMode.Default, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        tasks.Guard(nameof(tasks));
        return tasks.Select(x => FireTaskAndForget(x, asyncMode))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<TaskCompletionSource<T>> FireTasksAndForget<T>(IEnumerable<Func<Task<T>>> tasks, AsyncMode asyncMode = AsyncMode.Default, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        tasks.Guard(nameof(tasks));
        return tasks.Select(x => FireTaskAndForget(x, asyncMode))
            .ToList()
            .AsReadOnly();
    }

    public async Task ExecuteTaskOnThreadPoolAsync(Func<Task> task, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        await ExecuteTaskOnThreadPoolAsync(WrapTask(task));
    }

    public async Task<T> ExecuteTaskOnThreadPoolAsync<T>(Func<Task<T>> taskFunc, bool logException = true)
    {
        Task<T> task = await ExecuteOnThreadPoolAsync(taskFunc, CancellationToken.None);

        try
        {
            return await task;
        }
        catch (Exception ex) when (logException)
        {
            _logger.Log(ex);
            throw;
        }
    }

    public async Task<T> ExecuteOnThreadPoolAsync<T>(Func<T> func, CancellationToken cancellationToken, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0, bool logException = true)
    {
        if (Thread.CurrentThread.IsThreadPoolThread)
            return func();

        try
        {
            return await new TaskFactory(TaskScheduler.Default).StartNew(func, cancellationToken);
        }
        catch (Exception ex) when (logException)
        {
            _logger.Log(ex);
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
    public async Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken)
    {
        await ExecuteOnThreadPoolAsync(() => Task.Delay(millisecondsDelay, cancellationToken), cancellationToken);
    }

    private async Task ExecuteAndCatchAsync<T>(Func<T> func, TaskCompletionSource<T> taskCompletionSource, CancellationToken cancellationToken, AsyncMode asyncMode)
    {
        try
        {
            T result = asyncMode switch
            {
                AsyncMode.MainThread => await ExecuteTaskOnThreadPoolAsync(() => _dispatcher.InvokeOnMainThreadAsync(func), false),
                AsyncMode.ThreadPool => await ExecuteOnThreadPoolAsync(func, cancellationToken, logException: false),
                _ => func()
            };

            taskCompletionSource.SetResult(result);
        }
        catch (Exception ex)
        {
            _logger.Log(ex);
            taskCompletionSource.SetException(ex);
        }
    }

    private async Task ExecuteTaskAndCatchAsync<T>(Func<Task<T>> task, TaskCompletionSource<T> taskCompletionSource, AsyncMode asyncMode)
    {
        try
        {
            T result = asyncMode switch
            {
                AsyncMode.MainThread => await ExecuteTaskOnThreadPoolAsync(() => _dispatcher.InvokeOnMainThreadAsync(task), false),
                AsyncMode.ThreadPool => await ExecuteTaskOnThreadPoolAsync(task, false),
                _ => await task()
            };

            taskCompletionSource.SetResult(result);
        }
        catch (Exception ex)
        {
            _logger.Log(ex);
            taskCompletionSource.SetException(ex);
        }
    }
}