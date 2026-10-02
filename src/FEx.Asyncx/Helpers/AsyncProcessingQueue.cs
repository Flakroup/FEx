using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Asyncx.Utilities;
using FEx.Core.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Helpers;

public sealed class AsyncProcessingQueue : IDisposable
{
    private readonly ConcurrentQueue<TaskCompletionSource<bool>> _taskQueue = [];
    // One permit per queued gate: the processing loop sleeps on it instead of spinning while idle.
    private readonly SemaphoreSlim _queuedSignal = new(0);
    private volatile bool _disposed;
    private readonly FExSemaphoreSlim _signal;
    private readonly FExSemaphoreSlim _semaphore;
    private int _concurrencyLimit;
    private int _currentRunning;

    /// <summary>
    /// Dynamically updates the maximum allowed concurrency.
    /// When increasing, waiting tasks are released immediately in FIFO order.
    /// </summary>
    public uint ConcurrencyLimit
    {
        get => (uint)_concurrencyLimit;
        set
        {
#if NETSTANDARD
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(ConcurrencyLimit));
#else
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual((int)value, 0, nameof(ConcurrencyLimit));
#endif
            Interlocked.Exchange(ref _concurrencyLimit, (int)value);
            FExCoreStatics.AsyncHelper.FireTaskAndForget(TryReleasePollingAsync);
        }
    }

    private async Task TryReleasePollingAsync()
    {
        if (!await _semaphore.WaitAsync(TimeSpan.Zero))
            return;

        try
        {
            if (_signal.CurrentCount == 0)
                _signal.SafeRelease();
        }
        finally
        {
            _semaphore.SafeRelease();
        }
    }

    public int RunningCount => Volatile.Read(ref _currentRunning);

    public int QueuedCount => _taskQueue.Count;

    public AsyncProcessingQueue()
        : this(10)
    {
    }

    public AsyncProcessingQueue(uint limit)
    {
        _semaphore = new();
        _signal = new();
        ConcurrencyLimit = limit;

        FExCoreStatics.AsyncHelper.FireTaskAndForget(ProcessQueueAsync);
    }

    /// <summary>
    /// Schedules a task in FIFO order.
    /// </summary>
    /// <returns>Task that completes when the scheduled task finishes</returns>
    public Task EnqueueAsync(Func<Task> taskFunc) => EnqueueAsync(taskFunc, CancellationToken.None);

    public async Task EnqueueAsync(Func<Task> taskFunc, CancellationToken cancellationToken)
    {
        await GateAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await taskFunc();
        }
        finally
        {
            await OnTaskCompletedAsync();
        }
    }

    /// <summary>
    /// Schedules a task with result in FIFO order.
    /// </summary>
    /// <returns>Task that completes when the scheduled task finishes</returns>
    public Task<T> EnqueueAsync<T>(Func<Task<T>> taskFunc) => EnqueueAsync(taskFunc, CancellationToken.None);

    public async Task<T> EnqueueAsync<T>(Func<Task<T>> taskFunc, CancellationToken cancellationToken)
    {
        await GateAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await taskFunc();
        }
        finally
        {
            await OnTaskCompletedAsync();
        }
    }

    private async Task GateAsync(CancellationToken cancellationToken)
    {
        var gate = new AsyncTaskCompletionSource<bool>();
#if !NETSTANDARD2_0
        await
#endif
            using var registration = cancellationToken.Register(() => gate.TrySetCanceled(cancellationToken));

        _taskQueue.Enqueue(gate);
        _queuedSignal.Release();
        await gate.Task;
    }

    /// <summary>
    /// Releases waiting tasks while available concurrency slots exist.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            await _queuedSignal.WaitAsync();

            if (_disposed)
                return;

            await WaitWhileAboveLimitAsync();

            if (_taskQueue.TryDequeue(out var gate))
                ReleaseGate(gate);
        }
    }

    private async Task WaitWhileAboveLimitAsync()
    {
        while (RunningCount >= _concurrencyLimit)
            await _signal.WaitAsync();
    }

    private void ReleaseGate(TaskCompletionSource<bool> gate)
    {
        Interlocked.Increment(ref _currentRunning);

        if (!gate.TrySetResult(true))
            Interlocked.Decrement(ref _currentRunning);
    }

    /// <summary>
    /// Called when a task completes so waiting tasks can be released.
    /// </summary>
    private async Task OnTaskCompletedAsync()
    {
        Interlocked.Decrement(ref _currentRunning);
        await TryReleasePollingAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _queuedSignal.Release();
        _signal?.Dispose();
        _semaphore?.Dispose();
    }
}