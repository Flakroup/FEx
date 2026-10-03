using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.DependencyInjection.Tests;

/// <summary>
/// Pins the task-returning <c>WithWhenAllAsync</c> overloads: they must await the returned tasks (the generic overload
/// finished before the work did), return results in input order, honour the cancellation token, and keep compiling for
/// an async lambda without a result.
/// </summary>
public class WithWhenAllAsyncTests
{
    private static readonly int[] Values = [4, 3, 2, 1, 0];

    [Fact]
    public async Task Async_lambda_without_a_result_compiles_without_a_cast_and_is_awaited()
    {
        var done = new ConcurrentBag<int>();

        // No cast: this binds to the Task overload (OverloadResolutionPriority); without it the call is ambiguous with
        // the ValueTask overload (CS0121), which is the compile-time guard here.
        await Values.WithWhenAllAsync(async v =>
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
            done.Add(v);
        }, cancellationToken: TestContext.Current.CancellationToken);

        done.Count.ShouldBe(Values.Length);
    }

    [Theory]
    [InlineData(AsyncOptions.ImmediateStart)]
    [InlineData(AsyncOptions.None)]
    public async Task Task_of_result_overload_awaits_and_returns_results_in_input_order(AsyncOptions options)
    {
        var results = await Values.WithWhenAllAsync(async v =>
        {
            // The first value finishes last, so a wrong order would show.
            await Task.Delay(v * 30, TestContext.Current.CancellationToken);

            return v * 10;
        }, AsyncMode.Default, options, TestContext.Current.CancellationToken);

        results.ShouldBe([40, 30, 20, 10, 0]);
    }

    [Theory]
    [InlineData(AsyncOptions.ImmediateStart)]
    [InlineData(AsyncOptions.None)]
    public async Task ValueTask_of_result_overload_awaits_and_returns_results_in_input_order(AsyncOptions options)
    {
        Func<int, ValueTask<int>> work = v => new ValueTask<int>(WorkAsync(v));

        var results = await Values.WithWhenAllAsync(work, AsyncMode.Default, options, TestContext.Current.CancellationToken);

        results.ShouldBe([40, 30, 20, 10, 0]);
    }

    private static async Task<int> WorkAsync(int v)
    {
        await Task.Delay(v * 30, TestContext.Current.CancellationToken);

        return v * 10;
    }

    [Theory]
    [InlineData(AsyncOptions.ImmediateStart)]
    [InlineData(AsyncOptions.None)]
    public async Task Task_overload_with_a_cancelled_token_throws_and_runs_nothing(AsyncOptions options)
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var started = 0;

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await Values.WithWhenAllAsync(async _ =>
            {
                Interlocked.Increment(ref started);
                await Task.Yield();
            }, AsyncMode.Default, options, cts.Token));

        started.ShouldBe(0);
    }
}
