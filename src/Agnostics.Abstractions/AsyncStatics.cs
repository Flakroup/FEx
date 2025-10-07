using FEx.Abstractions.Enums;
using FEx.Abstractions.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Extensions.Helpers;

public class StaticAsyncHelper
{
    public static TimeSpan DefaultDelay { get; set; } = TimeSpan.FromMilliseconds(25); //todo move to conf class

    public static async Task ExecuteOnThreadPoolAsync(Action action,
                                                      AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart,
                                                      CancellationToken cancellationToken = default)
    {
        if (options.HasFlagFast(AsyncHelperOptions.ImmediateStart))
        {
            await ExecuteTaskOnThreadPoolAsync(() => Task.Run(action, cancellationToken), AsyncHelperOptions.None);

            return;
        }

        if (Thread.CurrentThread.IsThreadPoolThread)
            action();

        await new TaskFactory(TaskScheduler.Default).StartNew(action, cancellationToken);
    }

    public static async Task<T> ExecuteOnThreadPoolAsync<T>(Func<T> func,
                                                            AsyncHelperOptions options =
                                                                AsyncHelperOptions.ImmediateStart,
                                                            CancellationToken cancellationToken = default)
    {
        Type argumentType = typeof(T);

        if (argumentType == typeof(Task)
            || argumentType.IsGenericType && argumentType.GetGenericTypeDefinition() == typeof(Task<>))
            throw new ArgumentException("Invalid generic type. It cannot be Task or Task<T>.");

        if (options.HasFlagFast(AsyncHelperOptions.ImmediateStart))
            return await ExecuteTaskOnThreadPoolAsync(() => Task.Run(func, cancellationToken), AsyncHelperOptions.None);

        if (Thread.CurrentThread.IsThreadPoolThread)
            return func();

        return await new TaskFactory(TaskScheduler.Default).StartNew(func, cancellationToken);
    }

    public static async Task ExecuteTaskOnThreadPoolAsync(Func<Task> func,
                                                          AsyncHelperOptions options =
                                                              AsyncHelperOptions.ImmediateStart)
    {
        Func<Task> effectiveFunc = options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
            ? () => Task.Run(func)
            : func;

        if (Thread.CurrentThread.IsThreadPoolThread)
        {
            await effectiveFunc();

            return;
        }

        await await new TaskFactory(TaskScheduler.Default).StartNew(effectiveFunc);
    }

    public static async Task<T> ExecuteTaskOnThreadPoolAsync<T>(Func<Task<T>> func,
                                                                AsyncHelperOptions options =
                                                                    AsyncHelperOptions.ImmediateStart)
    {
        Func<Task<T>> effectiveFunc = options.HasFlagFast(AsyncHelperOptions.ImmediateStart)
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
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// The
    /// <paramref name="millisecondsDelay">millisecondsDelay</paramref> argument is less than -1.
    /// </exception>
    /// <exception cref="T:System.Threading.Tasks.TaskCanceledException">The task has been canceled.</exception>
    /// <exception cref="T:System.ObjectDisposedException">
    /// The provided
    /// <paramref name="cancellationToken">cancellationToken</paramref> has already been disposed.
    /// </exception>
    public static async Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(() => Task.Delay(millisecondsDelay, cancellationToken));

    /// <summary>Creates a cancellable task that completes after a specified time interval.</summary>
    /// <param name="delay">
    /// The time span to wait before completing the returned task, or
    /// <see langword="TimeSpan.FromMilliseconds(-1)" /> to wait indefinitely.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="delay" /> represents a negative time interval other than
    /// <see langword="TimeSpan.FromMilliseconds(-1)" />.
    /// -or-
    /// The <paramref name="delay" /> argument's <see cref="P:System.TimeSpan.TotalMilliseconds" /> property is greater
    /// than 4294967294 on .NET 6 and later versions, or <see cref="F:System.Int32.MaxValue">Int32.MaxValue</see> on all
    /// previous versions.
    /// </exception>
    /// <exception cref="T:System.Threading.Tasks.TaskCanceledException">The task has been canceled.</exception>
    /// <exception cref="T:System.ObjectDisposedException">
    /// The provided <paramref name="cancellationToken" /> has already been
    /// disposed.
    /// </exception>
    /// <returns>A task that represents the time delay.</returns>
    public static async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) =>
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
                                             Action action = null,
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
                                             Action action = null,
                                             double milliseconds = 0,
                                             CancellationToken cancellationToken = default) =>
        await ExecuteTaskOnThreadPoolAsync(() =>
            DelayUntilCoreAsync(predicate, action, GetDelayTimeSpan(milliseconds), cancellationToken));

    public static async Task DelayWithTimespanUntilAsync(Func<bool> predicate,
                                                         Action action = null,
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
                                                         Action action = null,
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
                                                      Action action = null,
                                                      CancellationToken cancellationToken = default)
    {
        TimeSpan delayTimeSpan = GetDelayTimeSpan(delayMilliseconds);

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
                                                      Action action = null,
                                                      CancellationToken cancellationToken = default)
    {
        delayTimeSpan = GetDelayTimeSpan(delayTimeSpan);

        await DelayAsync(delayTimeSpan.Value, cancellationToken);
        action?.Invoke();
    }

    public static object Wrap(Action action)
    {
        action();

        return null;
    }

    public static async Task<object> WrapTaskAsync(Func<Task> task)
    {
        await task();

        return null;
    }

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

    private static async Task DelayUntilCoreAsync(Func<bool> predicate,
                                                  Action action,
                                                  TimeSpan delayTimeSpan,
                                                  CancellationToken cancellationToken = default)
    {
        bool result = predicate is not null && predicate();

        if (!result)
            return;

        while (result && !cancellationToken.IsCancellationRequested)
        {
            await WaitAndInvokeActionAsync(delayTimeSpan, action, cancellationToken);
            result = predicate();
        }
    }

    private static async Task DelayUntilCoreAsync(Func<Task<bool>> predicate,
                                                  Action action,
                                                  TimeSpan delayTimeSpan,
                                                  CancellationToken cancellationToken = default)
    {
        bool result = predicate is not null && await predicate();

        if (!result)
            return;

        while (result && !cancellationToken.IsCancellationRequested)
        {
            await WaitAndInvokeActionAsync(delayTimeSpan, action, cancellationToken);
            result = await predicate();
        }
    }
}