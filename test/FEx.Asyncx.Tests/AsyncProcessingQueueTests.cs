using FEx.Asyncx.Helpers;
using Shouldly;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Asyncx.Tests;

public sealed class AsyncProcessingQueueTests
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private static TaskCompletionSource<bool> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// An idle queue with spare capacity used to spin its processing loop at 100% of a core (netstandard2.0).
    /// </summary>
    [Fact]
    public async Task IdleQueue_DoesNotBurnCpu()
    {
        var ct = TestContext.Current.CancellationToken;
        using var queue = new AsyncProcessingQueue(2);
        var process = Process.GetCurrentProcess();

        // Let the constructor's background loop start, then measure CPU over an idle window.
        await Task.Delay(200, ct);
        var before = process.TotalProcessorTime;
        await Task.Delay(1000, ct);
        process.Refresh();
        var burned = process.TotalProcessorTime - before;

        // A spinning loop burns ~1000ms of CPU in the 1s window; an idle one a few ms.
        burned.TotalMilliseconds.ShouldBeLessThan(400);
    }

    [Fact]
    public async Task EnqueuedWork_RunsAfterIdle()
    {
        var ct = TestContext.Current.CancellationToken;
        using var queue = new AsyncProcessingQueue(1);
        var ran = NewSignal();

        _ = queue.EnqueueAsync(() =>
        {
            ran.SetResult(true);

            return Task.CompletedTask;
        }, ct);

        (await ran.Task.WaitAsync(_timeout, ct)).ShouldBeTrue();
    }

    [Fact]
    public async Task ConcurrencyLimit_IsRespected_AndQueuedWorkRunsOnceSlotFrees()
    {
        var ct = TestContext.Current.CancellationToken;
        using var queue = new AsyncProcessingQueue(1);
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var secondStarted = NewSignal();

        var first = queue.EnqueueAsync(async () =>
        {
            firstStarted.SetResult(true);
            await releaseFirst.Task.WaitAsync(_timeout);
        }, ct);
        await firstStarted.Task.WaitAsync(_timeout, ct);

        var second = queue.EnqueueAsync(() =>
        {
            secondStarted.SetResult(true);

            return Task.CompletedTask;
        }, ct);

        secondStarted.Task.IsCompleted.ShouldBeFalse();
        queue.RunningCount.ShouldBe(1);

        releaseFirst.SetResult(true);
        await Task.WhenAll(first, second).WaitAsync(_timeout, ct);

        secondStarted.Task.IsCompletedSuccessfully.ShouldBeTrue();
    }
}
