using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace FEx.Agnostics.Abstractions;

public static class AsyncStatics
{
    public static TimeSpan DefaultDelay { get; set; } = TimeSpan.FromMilliseconds(25); //todo move to conf class

    public static Task ExecuteOnThreadPoolAsync(Action action) =>
        ExecuteOnThreadPoolAsync(action, AsyncOptions.ImmediateStart);

    public static Task ExecuteOnThreadPoolAsync(Action action, AsyncOptions options) =>
        ExecuteOnThreadPoolAsync(action, options, CancellationToken.None);

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

    public static Task<T> ExecuteOnThreadPoolAsync<T>(Func<T> func) =>
        ExecuteOnThreadPoolAsync(func, AsyncOptions.ImmediateStart);

    public static Task<T> ExecuteOnThreadPoolAsync<T>(Func<T> func, AsyncOptions options) =>
        ExecuteOnThreadPoolAsync(func, options, CancellationToken.None);

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

    public static Task ExecuteTaskOnThreadPoolAsync(Func<Task> func) =>
        ExecuteTaskOnThreadPoolAsync(func, AsyncOptions.ImmediateStart);

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

    public static Task<T> ExecuteTaskOnThreadPoolAsync<T>(Func<Task<T>> func) =>
        ExecuteTaskOnThreadPoolAsync(func, AsyncOptions.ImmediateStart);

    public static async Task<T> ExecuteTaskOnThreadPoolAsync<T>(Func<Task<T>> func, AsyncOptions options)
    {
        var effectiveFunc = options.HasFlagFast(AsyncOptions.ImmediateStart)
            ? () => Task.Run(func)
            : func;

        if (Thread.CurrentThread.IsThreadPoolThread)
            return await effectiveFunc();

        return await await new TaskFactory(TaskScheduler.Default).StartNew(effectiveFunc);
    }

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
    public static Task DelayAsync(int millisecondsDelay) => DelayAsync(millisecondsDelay, CancellationToken.None);

    public static async Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken) =>
        await ExecuteTaskOnThreadPoolAsync(() => Task.Delay(millisecondsDelay, cancellationToken));

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
    public static Task DelayAsync(TimeSpan delay) => DelayAsync(delay, CancellationToken.None);

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

    public static async Task DelayUntilAsync(Func<Task<bool>> predicate,
                                             Action? action = null,
                                             double milliseconds = 0,
                                             CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(() =>
            DelayUntilCoreAsync(predicate, action, GetDelayTimeSpan(milliseconds), cancellationToken));

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

    public static async Task WaitAndInvokeActionAsync(TimeSpan? delayTimeSpan = null,
                                                      Action? action = null,
                                                      CancellationToken cancellationToken = default)
    {
        delayTimeSpan = GetDelayTimeSpan(delayTimeSpan);

        await DelayAsync(delayTimeSpan.Value, cancellationToken);
        action?.Invoke();
    }

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