using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Asyncx.Utilities;
using FEx.Core.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD2_0
using System.Collections.Concurrent;

#else
using System.Threading.Channels;
#endif

namespace FEx.Asyncx.Helpers;

public sealed class AsyncProcessingQueue : IDisposable
{
#if NETSTANDARD2_0
    private readonly ConcurrentQueue<TaskCompletionSource<bool>> _taskQueue;
#else
    private readonly Channel<TaskCompletionSource<bool>> _taskChannel;
#endif
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

    public int QueuedCount
    {
#if NETSTANDARD2_0
        get => _taskQueue.Count;
#else
        get => _taskChannel.Reader.Count;
#endif
    }

    public AsyncProcessingQueue() : this(10) { }

    public AsyncProcessingQueue(uint limit)
    {
        _semaphore = new();
        _signal = new();
        ConcurrencyLimit = limit;

#if NETSTANDARD2_0
        _taskQueue = [];
#else
        _taskChannel = Channel.CreateUnbounded<TaskCompletionSource<bool>>();
#endif

        FExCoreStatics.AsyncHelper.FireTaskAndForget(ProcessQueueAsync);
    }

    /// <summary>
    /// Schedules a task in FIFO order.
    /// </summary>
    /// <returns>Task that completes when the scheduled task finishes</returns>
    public Task EnqueueAsync(Func<Task> taskFunc) => EnqueueAsync(taskFunc, default);

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
    public Task<T> EnqueueAsync<T>(Func<Task<T>> taskFunc) => EnqueueAsync(taskFunc, default);

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

#if NETSTANDARD2_0
        _taskQueue.Enqueue(gate);
#else
        _taskChannel.Writer.TryWrite(gate);
#endif
        await gate.Task;
    }

    /// <summary>
    /// Releases waiting tasks while available concurrency slots exist.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
#if NETSTANDARD2_0
        while (true)
        {
            await WaitWhileAboveLimitAsync();

            while (QueuedCount > 0
                   && _currentRunning < _concurrencyLimit
                   && _taskQueue.TryDequeue(out var gate))
                ReleaseGate(gate);
        }
#else
        await foreach (var gate in _taskChannel.Reader.ReadAllAsync())
        {
            await WaitWhileAboveLimitAsync();

            ReleaseGate(gate);
        }
#endif
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
#if !NETSTANDARD2_0
        _taskChannel.Writer.TryComplete();
#endif
        _signal?.Dispose();
        _semaphore?.Dispose();
    }
}