using FEx.Abstractions;
using FEx.Extensions;
using FEx.Fundamentals.Enums;
using FEx.Logging.Abstractions;
using GuardNet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Fundamentals.Helpers;

public class AsyncHelper
{
    private readonly ITasksInfoSubject _tasksInfoSubject;
    private readonly IFExDispatcher _dispatcher;
    private readonly ILogger<AsyncHelper> _logger;

    public TimeSpan DefaultDelayTimeSpan { get; } = TimeSpan.FromMilliseconds(15);

    public AsyncHelper(IFExDispatcher dispatcher, ILogger<AsyncHelper> logger, ITasksInfoSubject tasksInfoSubject)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _tasksInfoSubject = tasksInfoSubject;
    }

    public TaskWrapper FireAndForget(Action action,
                                     AsyncMode asyncMode = AsyncMode.Default,
                                     CancellationToken cancellationToken = default)
    {
        action.Guard(nameof(action));
        var taskWrapper = new TaskWrapper();
        taskWrapper.SetTask(() => ExecuteAndCatchAsync(() => Wrap(action), taskWrapper, asyncMode, cancellationToken));

        return taskWrapper;
    }

    public TaskWrapper<T> FireAndForget<T>(Func<T> func,
                                           AsyncMode asyncMode = AsyncMode.Default,
                                           CancellationToken cancellationToken = default)
    {
        func.Guard(nameof(func));
        var taskWrapper = new TaskWrapper<T>();
        taskWrapper.SetTask(() => ExecuteAndCatchAsync(func, taskWrapper, asyncMode, cancellationToken));

        return taskWrapper;
    }

    public TaskWrapper FireTaskAndForget(Func<Task> task, AsyncMode asyncMode = AsyncMode.Default)
    {
        task.Guard(nameof(task));
        var taskWrapper = new TaskWrapper();
        taskWrapper.SetTask(() => ExecuteTaskAndCatchAsync(() => WrapTaskAsync(task), taskWrapper, asyncMode));

        return taskWrapper;
    }

    public TaskWrapper<T> FireTaskAndForget<T>(Func<Task<T>> task, AsyncMode asyncMode = AsyncMode.Default)
    {
        task.Guard(nameof(task));
        var taskWrapper = new TaskWrapper<T>();
        taskWrapper.SetTask(() => ExecuteTaskAndCatchAsync(task, taskWrapper, asyncMode));

        return taskWrapper;
    }

    public IReadOnlyList<TaskWrapper> FireTasksAndForget(IEnumerable<Func<Task>> tasks,
                                                         AsyncMode asyncMode = AsyncMode.Default)
    {
        Guard.For(() => tasks?.Any() != true,
            new ArgumentNullException(nameof(tasks), $"The {nameof(tasks)} cannot be null or empty."));

        return tasks.Select(x => FireTaskAndForget(x, asyncMode)).ToList().AsReadOnly();
    }

    public IReadOnlyList<TaskWrapper<T>> FireTasksAndForget<T>(IEnumerable<Func<Task<T>>> tasks,
                                                               AsyncMode asyncMode = AsyncMode.Default)
    {
        tasks.Guard(nameof(tasks));

        return tasks.Select(x => FireTaskAndForget(x, asyncMode)).ToList().AsReadOnly();
    }

    public async Task ExecuteTaskOnThreadPoolAsync(Func<Task> task, bool logException = true) =>
        await ExecuteTaskOnThreadPoolAsync(() => WrapTaskAsync(task), logException);

    public async Task<T> ExecuteTaskOnThreadPoolAsync<T>(Func<Task<T>> taskFunc, bool logException = true)
    {
        //todo enhance with valueTasks
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
                                                     bool logException = true,
                                                     CancellationToken cancellationToken = default)
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
    /// <paramref name="millisecondsDelay">millisecondsDelay</paramref> argument is less than -1.
    /// </exception>
    /// <exception cref="T:System.Threading.Tasks.TaskCanceledException">The task has been canceled.</exception>
    /// <exception cref="T:System.ObjectDisposedException">
    ///     The provided
    /// <paramref name="cancellationToken">cancellationToken</paramref> has already been disposed.
    /// </exception>
    public async Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(() => Task.Delay(millisecondsDelay, cancellationToken));

    /// <summary>Creates a cancellable task that completes after a specified time interval.</summary>
    /// <param name="delay">
    ///     The time span to wait before completing the returned task, or
    /// <see langword="TimeSpan.FromMilliseconds(-1)" /> to wait indefinitely.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="delay" /> represents a negative time interval other than
    /// <see langword="TimeSpan.FromMilliseconds(-1)" />.
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
    public async Task DelayAsync(TimeSpan delay,
                                 bool logException = true,
                                 CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(() => Task.Delay(delay, cancellationToken), logException);

    /// <summary>
    ///     Waits asynchronously the specified amount of milliseconds.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <param name="logException">Log exception</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="action">The action to invoke after awaited amount of time.</param>
    /// <param name="milliseconds">The amount of time in milliseconds to await.</param>
    /// <returns>
    ///     Task
    /// </returns>
    public async Task DelayUntilAsync(Func<bool> predicate,
                                      Action action = null,
                                      double milliseconds = 0,
                                      bool logException = true,
                                      CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(
            () => InternalDelayUntilAsync(predicate, action, milliseconds, logException, cancellationToken),
            logException);

    public async Task DelayUntilAsync(Func<Task<bool>> predicate,
                                      Action action = null,
                                      double milliseconds = 0,
                                      bool logException = true,
                                      CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(
            () => InternalDelayUntilAsync(predicate, action, milliseconds, logException, cancellationToken),
            logException);

    public async Task DelayUntilWithTimeSpanAsync(Func<bool> predicate,
                                                  Action action = null,
                                                  TimeSpan? timeSpan = null,
                                                  bool logException = true,
                                                  CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(
            () => InternalDelayUntilAsync(predicate, action, timeSpan, logException, cancellationToken), logException);

    public async Task DelayUntilWithTimeSpanAsync(Func<Task<bool>> predicate,
                                                  Action action = null,
                                                  TimeSpan? timeSpan = null,
                                                  bool logException = true,
                                                  CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(
            () => InternalDelayUntilAsync(predicate, action, timeSpan, logException, cancellationToken), logException);

    /// <summary>
    ///     Waits asynchronously the specified amount of milliseconds and invokes action.
    /// </summary>
    /// <param name="delayMilliseconds">The milliseconds.</param>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task WaitAndInvokeActionAsync(double delayMilliseconds = 0,
                                               Action action = null,
                                               bool logException = true,
                                               CancellationToken cancellationToken = default)
    {
        TimeSpan delayTimeSpan = delayMilliseconds < 1
            ? DefaultDelayTimeSpan
            : TimeSpan.FromMilliseconds(delayMilliseconds);

        try
        {
            await DelayAsync(delayTimeSpan, logException, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            //ignored
        }

        action?.Invoke();
    }

    public async Task WaitAndInvokeActionAsync(TimeSpan? delayTimeSpan = null,
                                               Action action = null,
                                               bool logException = true,
                                               CancellationToken cancellationToken = default)
    {
        if (delayTimeSpan is null
            || delayTimeSpan.Value <= TimeSpan.Zero)
            delayTimeSpan = DefaultDelayTimeSpan;

        await DelayAsync(delayTimeSpan.Value, logException, cancellationToken);
        action?.Invoke();
    }

    private static object Wrap(Action action)
    {
        action();

        return null;
    }

    private static async Task<object> WrapTaskAsync(Func<Task> task)
    {
        await task();

        return null;
    }

    private static void SetResult<T>(T result, ITaskWrapper taskWrapper)
    {
        switch (taskWrapper)
        {
            case TaskWrapper<T> typedWrapper:
                typedWrapper.SetResult(result);

                break;
            case TaskWrapper wrapper:
                wrapper.SetResult();

                break;
            default:
                throw new InvalidOperationException($"{taskWrapper.GetType().FullName} is not handled");
        }
    }

    private async Task InternalDelayUntilAsync(Func<bool> predicate,
                                               Action action = null,
                                               double delayMilliseconds = 0,
                                               bool logException = true,
                                               CancellationToken cancellationToken = default)
    {
        if (predicate is null
            || !predicate())
            return;

        while (predicate()
               && !cancellationToken.IsCancellationRequested)
            await WaitAndInvokeActionAsync(delayMilliseconds, action, logException, cancellationToken);
    }

    private async Task InternalDelayUntilAsync(Func<Task<bool>> predicate,
                                               Action action = null,
                                               double delayMilliseconds = 0,
                                               bool logException = true,
                                               CancellationToken cancellationToken = default)
    {
        bool result = predicate is not null && await predicate();

        if (!result)
            return;

        while (result && !cancellationToken.IsCancellationRequested)
        {
            await WaitAndInvokeActionAsync(delayMilliseconds, action, logException, cancellationToken);
            result = await predicate();
        }
    }

    private async Task InternalDelayUntilAsync(Func<bool> predicate,
                                               Action action = null,
                                               TimeSpan? delayTimeSpan = null,
                                               bool logException = true,
                                               CancellationToken cancellationToken = default)
    {
        if (predicate is null
            || !predicate())
            return;

        while (predicate()
               && !cancellationToken.IsCancellationRequested)
            await WaitAndInvokeActionAsync(delayTimeSpan, action, logException, cancellationToken);
    }

    private async Task InternalDelayUntilAsync(Func<Task<bool>> predicate,
                                               Action action = null,
                                               TimeSpan? delayTimeSpan = null,
                                               bool logException = true,
                                               CancellationToken cancellationToken = default)
    {
        bool result = predicate is not null && await predicate();

        if (!result)
            return;

        while (result && !cancellationToken.IsCancellationRequested)
        {
            await WaitAndInvokeActionAsync(delayTimeSpan, action, logException, cancellationToken);
            result = await predicate();
        }
    }

    private async Task<T> ExecuteAndCatchAsync<T>(Func<T> func,
                                                  ITaskWrapper taskWrapper,
                                                  AsyncMode asyncMode,
                                                  CancellationToken cancellationToken)
    {
        _tasksInfoSubject.AddTask(taskWrapper);
        T result = default;

        try
        {
            result = asyncMode switch
            {
                AsyncMode.MainThread => await ExecuteTaskOnThreadPoolAsync(
                    () => _dispatcher.InvokeOnMainThreadAsync(func), false),
                AsyncMode.ThreadPool => await ExecuteOnThreadPoolAsync(func, false, cancellationToken),
                _ => await Task.Run(func, cancellationToken)
            };

            SetResult(result, taskWrapper);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex);
            taskWrapper.SetException(ex);
        }

        _tasksInfoSubject.RemoveTask(taskWrapper);

        return result;
    }

    private async Task<T> ExecuteTaskAndCatchAsync<T>(Func<Task<T>> task, ITaskWrapper taskWrapper, AsyncMode asyncMode)
    {
        _tasksInfoSubject.AddTask(taskWrapper);
        T result = default;

        try
        {
            result = asyncMode switch
            {
                AsyncMode.MainThread => await ExecuteTaskOnThreadPoolAsync(
                    () => _dispatcher.InvokeOnMainThreadAsync(task), false),
                AsyncMode.ThreadPool => await ExecuteTaskOnThreadPoolAsync(task, false),
                _ => await task()
            };

            SetResult(result, taskWrapper);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex);
            taskWrapper.SetException(ex);
        }

        _tasksInfoSubject.RemoveTask(taskWrapper);

        return result;
    }
}