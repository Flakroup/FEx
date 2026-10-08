using FEx.Asyncx.Helpers;
using Shouldly;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Asyncx.Tests;

[Collection(NonParallelCollection.Name)]
public sealed class AsyncProcessingQueueCpuTests
{
    private static readonly TimeSpan _window = TimeSpan.FromSeconds(1);

    /// <summary>
    /// A processing loop that busy-waits without ever reaching an await burns a core, and no iteration count can see
    /// it. The process-wide CPU of a window with an idle queue is compared with the same window without one, in a
    /// collection that runs alone, so only the queue's own cost is left.
    /// </summary>
    [Fact]
    public async Task IdleQueue_DoesNotBurnCpu()
    {
        var ct = TestContext.Current.CancellationToken;

        // Warm up everything the queue touches, so JIT does not land in the measured window.
        using (var warmup = new AsyncProcessingQueue(2))
            await warmup.EnqueueAsync(() => Task.CompletedTask, ct);

        var baseline = await MeasureCpuAsync(ct);

        using var queue = new AsyncProcessingQueue(2);
        await Task.Delay(200, ct);
        var withQueue = await MeasureCpuAsync(ct);

        // A spinning loop adds ~1000ms of CPU to the 1s window; a parked one adds nothing measurable.
        (withQueue - baseline).TotalMilliseconds.ShouldBeLessThan(400);
    }

    private static async Task<TimeSpan> MeasureCpuAsync(CancellationToken ct)
    {
        using var process = Process.GetCurrentProcess();
        var before = process.TotalProcessorTime;

        await Task.Delay(_window, ct);

        process.Refresh();

        return process.TotalProcessorTime - before;
    }
}
