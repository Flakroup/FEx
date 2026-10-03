using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace FEx.Agnostics.Abstractions;

/// <summary>Helpers for running work on the thread pool and for delaying, polling and waiting asynchronously.</summary>
public static class AsyncStatics
{
    /// <summary>Gets or sets the polling delay used when no valid delay is supplied.</summary>
    public static TimeSpan DefaultDelay { get; set; } = TimeSpan.FromMilliseconds(25); //todo move to conf class

    /// <summary>Runs an action on the thread pool, starting it immediately</summary>
    /// <param name="action">The action to run.</param>
    /// <returns>A task that completes when the action has run.</returns>
    public static Task ExecuteOnThreadPoolAsync(Action action) =>
        ExecuteOnThreadPoolAsync(action, AsyncOptions.ImmediateStart);

    /// <summary>Runs an action on the thread pool</summary>
    /// <param name="action">The action to run.</param>
    /// <param name="options">How the work is started.</param>
    /// <returns>A task that completes when the action has run.</returns>
    public static Task ExecuteOnThreadPoolAsync(Action action, AsyncOptions options) =>
        ExecuteOnThreadPoolAsync(action, options, CancellationToken.None);

    /// <summary>Runs an action on the thread pool; when not starting immediately and already on a pool thread, the action is invoked inline first</summary>
    /// <param name="action">The action to run.</param>
    /// <param name="options">How the work is started.</param>
    /// <param name="cancellationToken">Token used to cancel the work before it starts.</param>
    /// <returns>A task that completes when the action has run.</returns>
    public static async Task ExecuteOnThreadPoolAsync(Action action,
                                                      AsyncOptions options,
                                                      CancellationToken cancellationToken)
    {
        if (options.HasFlagFast(AsyncOptions.ImmediateStart))
        {
            await ExecuteTaskOnThreadPoolAsync(() => Task.Run(action, cancellationToken), AsyncOptions.None);

            return;
        }

        if (Thread.CurrentThread.IsThreadPoolThread)
            action();

        await new TaskFactory(TaskScheduler.Default).StartNew(action, cancellationToken);
    }

    /// <summary>Runs a function on the thread pool, starting it immediately</summary>
    /// <typeparam name="T">The function result type.</typeparam>
    /// <param name="func">The function to run.</param>
    /// <returns>A task that yields the function result.</returns>
    public static Task<T> ExecuteOnThreadPoolAsync<T>(Func<T> func) =>
        ExecuteOnThreadPoolAsync(func, AsyncOptions.ImmediateStart);

    /// <summary>Runs a function on the thread pool</summary>
    /// <typeparam name="T">The function result type.</typeparam>
    /// <param name="func">The function to run.</param>
    /// <param name="options">How the work is started.</param>
    /// <returns>A task that yields the function result.</returns>
    public static Task<T> ExecuteOnThreadPoolAsync<T>(Func<T> func, AsyncOptions options) =>
        ExecuteOnThreadPoolAsync(func, options, CancellationToken.None);

    /// <summary>Runs a function on the thread pool</summary>
    /// <typeparam name="T">The function result type, which must not be a task type.</typeparam>
    /// <param name="func">The function to run.</param>
    /// <param name="options">How the work is started.</param>
    /// <param name="cancellationToken">Token used to cancel the work before it starts.</param>
    /// <returns>A task that yields the function result.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="T" /> is <see cref="System.Threading.Tasks.Task" /> or <see cref="System.Threading.Tasks.Task{TResult}" />.</exception>
    public static async Task<T> ExecuteOnThreadPoolAsync<T>(Func<T> func,
                                                            AsyncOptions options,
                                                            CancellationToken cancellationToken)
    {
        var argumentType = typeof(T);

        if (argumentType == typeof(Task)
            || argumentType.IsGenericType && argumentType.GetGenericTypeDefinition() == typeof(Task<>))
            throw new ArgumentException("Invalid generic type. It cannot be Task or Task<T>.");

        if (options.HasFlagFast(AsyncOptions.ImmediateStart))
            return await ExecuteTaskOnThreadPoolAsync(() => Task.Run(func, cancellationToken), AsyncOptions.None);

        if (Thread.CurrentThread.IsThreadPoolThread)
            return func();

        return await new TaskFactory(TaskScheduler.Default).StartNew(func, cancellationToken);
    }

    /// <summary>Runs an asynchronous delegate on the thread pool, starting it immediately</summary>
    /// <param name="func">Factory of the task to run.</param>
    /// <returns>A task that completes when the delegate's task has completed.</returns>
    public static Task ExecuteTaskOnThreadPoolAsync(Func<Task> func) =>
        ExecuteTaskOnThreadPoolAsync(func, AsyncOptions.ImmediateStart);

    /// <summary>Runs an asynchronous delegate on the thread pool, running inline when already on a pool thread</summary>
    /// <param name="func">Factory of the task to run.</param>
    /// <param name="options">How the work is started.</param>
    /// <returns>A task that completes when the delegate's task has completed.</returns>
    public static async Task ExecuteTaskOnThreadPoolAsync(Func<Task> func, AsyncOptions options)
    {
        var effectiveFunc = options.HasFlagFast(AsyncOptions.ImmediateStart)
            ? () => Task.Run(func)
            : func;

        if (Thread.CurrentThread.IsThreadPoolThread)
        {
            await effectiveFunc();

            return;
        }

        await await new TaskFactory(TaskScheduler.Default).StartNew(effectiveFunc);
    }

    /// <summary>Runs an asynchronous delegate on the thread pool, starting it immediately</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="func">Factory of the task to run.</param>
    /// <returns>A task that yields the delegate's result.</returns>
    public static Task<T> ExecuteTaskOnThreadPoolAsync<T>(Func<Task<T>> func) =>
        ExecuteTaskOnThreadPoolAsync(func, AsyncOptions.ImmediateStart);

    /// <summary>Runs an asynchronous delegate on the thread pool, running inline when already on a pool thread</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="func">Factory of the task to run.</param>
    /// <param name="options">How the work is started.</param>
    /// <returns>A task that yields the delegate's result.</returns>
    public static async Task<T> ExecuteTaskOnThreadPoolAsync<T>(Func<Task<T>> func, AsyncOptions options)
    {
        var effectiveFunc = options.HasFlagFast(AsyncOptions.ImmediateStart)
            ? () => Task.Run(func)
            : func;

        if (Thread.CurrentThread.IsThreadPoolThread)
            return await effectiveFunc();

        return await await new TaskFactory(TaskScheduler.Default).StartNew(effectiveFunc);
    }

    /// <summary>Creates a task that completes after a time delay and cannot be cancelled.</summary>
    /// <param name="millisecondsDelay">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
    /// <returns>A task that represents the time delay.</returns>
    public static Task DelayAsync(int millisecondsDelay) => DelayAsync(millisecondsDelay, CancellationToken.None);

    /// <summary>Creates a cancellable task that completes after a time delay.</summary>
    /// <param name="millisecondsDelay">
    /// The number of milliseconds to wait before completing the returned task, or -1 to wait
    /// indefinitely.
    /// </param>
    /// <param name="cancellationToken">The cancellation token that will be checked prior to completing the returned task.</param>
    /// <returns>A task that represents the time delay.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The
    /// <paramref name="millisecondsDelay">millisecondsDelay</paramref> argument is less than -1.
    /// </exception>
    /// <exception cref="TaskCanceledException">The task has been canceled.</exception>
    /// <exception cref="ObjectDisposedException">
    /// The provided
    /// <paramref name="cancellationToken">cancellationToken</paramref> has already been disposed.
    /// </exception>
    public static async Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken) =>
        await ExecuteTaskOnThreadPoolAsync(() => Task.Delay(millisecondsDelay, cancellationToken));

    /// <summary>Creates a task that completes after a specified time interval and cannot be cancelled.</summary>
    /// <param name="delay">The time span to wait, or <c>TimeSpan.FromMilliseconds(-1)</c> to wait indefinitely.</param>
    /// <returns>A task that represents the time delay.</returns>
    public static Task DelayAsync(TimeSpan delay) => DelayAsync(delay, CancellationToken.None);

    /// <summary>Creates a cancellable task that completes after a specified time interval.</summary>
    /// <param name="delay">
    /// The time span to wait before completing the returned task, or
    /// <see langword="TimeSpan.FromMilliseconds(-1)" /> to wait indefinitely.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="delay" /> represents a negative time interval other than
    /// <see langword="TimeSpan.FromMilliseconds(-1)" />.
    /// -or-
    /// The <paramref name="delay" /> argument's <see cref="P:System.TimeSpan.TotalMilliseconds" /> property is greater
    /// than 4294967294 on .NET 6 and later versions, or <see cref="F:System.Int32.MaxValue">Int32.MaxValue</see> on all
    /// previous versions.
    /// </exception>
    /// <exception cref="TaskCanceledException">The task has been canceled.</exception>
    /// <exception cref="ObjectDisposedException">
    /// The provided <paramref name="cancellationToken" /> has already been
    /// disposed.
    /// </exception>
    /// <returns>A task that represents the time delay.</returns>
    public static async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        await ExecuteTaskOnThreadPoolAsync(() => SafeDelayAsync(delay, cancellationToken));

    /// <summary>
    /// Waits asynchronously the specified amount of milliseconds.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="action">The action to invoke after awaited amount of time.</param>
    /// <param name="milliseconds">The amount of time in milliseconds to await.</param>
    /// <returns>
    /// Task
    /// </returns>
    public static async Task DelayUntilAsync(Func<bool> predicate,
                                             Action? action = null,
                                             double milliseconds = 0,
                                             CancellationToken cancellationToken = default)
    {
        if (predicate is null
            || !predicate())
            return;

        await ExecuteTaskOnThreadPoolAsync(() =>
            DelayUntilCoreAsync(predicate, action, GetDelayTimeSpan(milliseconds), cancellationToken));
    }

    /// <summary>Polls an asynchronous predicate until it returns false, invoking an optional action after each interval</summary>
    /// <param name="predicate">The asynchronous condition; polling continues while it returns true.</param>
    /// <param name="action">The action invoked after each polling interval.</param>
    /// <param name="milliseconds">The polling interval in milliseconds; <see cref="DefaultDelay" /> when below 1.</param>
    /// <param name="cancellationToken">Token used to cancel the wait.</param>
    /// <returns>A task that completes when the condition is no longer met.</returns>
    public static async Task DelayUntilAsync(Func<Task<bool>> predicate,
                                             Action? action = null,
                                             double milliseconds = 0,
                                             CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(() =>
            DelayUntilCoreAsync(predicate, action, GetDelayTimeSpan(milliseconds), cancellationToken));

    /// <summary>Polls a predicate at a time-span interval until it returns false, invoking an optional action after each interval</summary>
    /// <param name="predicate">The condition; polling continues while it returns true.</param>
    /// <param name="action">The action invoked after each polling interval.</param>
    /// <param name="timeSpan">The polling interval; <see cref="DefaultDelay" /> when null or not positive.</param>
    /// <param name="cancellationToken">Token used to cancel the wait.</param>
    /// <returns>A task that completes when the condition is no longer met.</returns>
    public static async Task DelayWithTimespanUntilAsync(Func<bool> predicate,
                                                         Action? action = null,
                                                         TimeSpan? timeSpan = null,
                                                         CancellationToken cancellationToken = default)
    {
        if (predicate is null
            || !predicate())
            return;

        await ExecuteTaskOnThreadPoolAsync(() =>
            DelayUntilCoreAsync(predicate, action, GetDelayTimeSpan(timeSpan), cancellationToken));
    }

    /// <summary>Polls an asynchronous predicate at a time-span interval until it returns false, invoking an optional action after each interval</summary>
    /// <param name="predicate">The asynchronous condition; polling continues while it returns true.</param>
    /// <param name="action">The action invoked after each polling interval.</param>
    /// <param name="timeSpan">The polling interval; <see cref="DefaultDelay" /> when null or not positive.</param>
    /// <param name="cancellationToken">Token used to cancel the wait.</param>
    /// <returns>A task that completes when the condition is no longer met.</returns>
    public static async Task DelayWithTimespanUntilAsync(Func<Task<bool>> predicate,
                                                         Action? action = null,
                                                         TimeSpan? timeSpan = null,
                                                         CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(() =>
            DelayUntilCoreAsync(predicate, action, GetDelayTimeSpan(timeSpan), cancellationToken));

    /// <summary>
    /// Waits asynchronously the specified amount of milliseconds and invokes action.
    /// </summary>
    /// <param name="delayMilliseconds">The milliseconds.</param>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task WaitAndInvokeActionAsync(double delayMilliseconds = 0,
                                                      Action? action = null,
                                                      CancellationToken cancellationToken = default)
    {
        var delayTimeSpan = GetDelayTimeSpan(delayMilliseconds);

        try
        {
            await DelayAsync(delayTimeSpan, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            //ignored
        }

        action?.Invoke();
    }

    /// <summary>Waits for a time span and then invokes an action</summary>
    /// <param name="delayTimeSpan">The delay; <see cref="DefaultDelay" /> when null or not positive.</param>
    /// <param name="action">The action to invoke after the delay.</param>
    /// <param name="cancellationToken">Token used to cancel the delay.</param>
    /// <returns>A task that completes after the action has been invoked.</returns>
    public static async Task WaitAndInvokeActionAsync(TimeSpan? delayTimeSpan = null,
                                                      Action? action = null,
                                                      CancellationToken cancellationToken = default)
    {
        delayTimeSpan = GetDelayTimeSpan(delayTimeSpan);

        await DelayAsync(delayTimeSpan.Value, cancellationToken);
        action?.Invoke();
    }

    /// <summary>Runs an action on a new thread and blocks until the thread ends.</summary>
    /// <param name="action">The action to run.</param>
    /// <param name="state">The apartment state to set before starting; the default when null.</param>
    /// <param name="isBackground">Whether the thread is a background thread; unchanged when null.</param>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public static void RunAsThread(Action action, ApartmentState? state = null, bool? isBackground = false)
    {
        var thread = new Thread(() => action());

        if (state.HasValue)
            thread.SetApartmentState(state.Value);

        thread.Start();

        if (isBackground.HasValue)
            thread.IsBackground = isBackground.Value;

        thread.Join();
    }

    private static async Task SafeDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            //ignore
        }
    }

    private static TimeSpan GetDelayTimeSpan(double delayMilliseconds) =>
        delayMilliseconds < 1
            ? DefaultDelay
            : TimeSpan.FromMilliseconds(delayMilliseconds);

    private static TimeSpan GetDelayTimeSpan(TimeSpan? delayTimeSpan)
    {
        if (!delayTimeSpan.HasValue
            || delayTimeSpan.Value <= TimeSpan.Zero)
            delayTimeSpan = DefaultDelay;

        return delayTimeSpan.Value;
    }

    private static async Task DelayUntilCoreAsync(Func<bool>? predicate,
                                                  Action? action,
                                                  TimeSpan delayTimeSpan,
                                                  CancellationToken cancellationToken = default)
    {
        var result = predicate is not null && predicate();

        if (!result)
            return;

        while (result && !cancellationToken.IsCancellationRequested)
        {
            await WaitAndInvokeActionAsync(delayTimeSpan, action, cancellationToken);
            result = predicate is not null && predicate();
        }
    }

    private static async Task DelayUntilCoreAsync(Func<Task<bool>>? predicate,
                                                  Action? action,
                                                  TimeSpan delayTimeSpan,
                                                  CancellationToken cancellationToken = default)
    {
        var result = predicate is not null && await predicate();

        if (!result)
            return;

        while (result && !cancellationToken.IsCancellationRequested)
        {
            await WaitAndInvokeActionAsync(delayTimeSpan, action, cancellationToken);
            result = predicate is not null && await predicate();
        }
    }
}