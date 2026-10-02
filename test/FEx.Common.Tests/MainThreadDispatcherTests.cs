using FEx.Common.Implementations;
using FEx.Core.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Common.Tests;

/// <summary>
/// With no captured main-thread SynchronizationContext (console/test host, before SetMainThread) the async
/// invoke methods used to skip the delegate and report success. They must run it inline instead.
/// </summary>
public sealed class MainThreadDispatcherTests
{
    private sealed class RecordingContext : SynchronizationContext
    {
        public int Posts;
        public int Sends;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref Posts);
            d(state);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref Sends);
            d(state);
        }
    }

    private static MainThreadDispatcher CreateWith(SynchronizationContext? context)
    {
        var provider = Substitute.For<IMainThreadContextProvider>();
        provider.Context.Returns(context!);

        return new(provider, Substitute.For<ILogger>(), Substitute.For<IDeadlockMonitor>(),
            Substitute.For<IAppThreadingSettings>(), Substitute.For<IStackTraceProvider>());
    }

    private static MainThreadDispatcher CreateWithoutContext()
    {
        var provider = Substitute.For<IMainThreadContextProvider>();
        provider.Context.Returns((SynchronizationContext)null!);
        var settings = Substitute.For<IAppThreadingSettings>();

        return new(provider, Substitute.For<ILogger>(), Substitute.For<IDeadlockMonitor>(), settings,
            Substitute.For<IStackTraceProvider>());
    }

    [Fact]
    public void BeginInvokeOnMainThread_RunsAction_WhenNoContext()
    {
        var ran = false;

        CreateWithoutContext().BeginInvokeOnMainThread(() => ran = true);

        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_Func_ReturnsRealResult_WhenNoContext() =>
        (await CreateWithoutContext().InvokeOnMainThreadAsync(() => 42)).ShouldBe(42);

    [Fact]
    public async Task InvokeOnMainThreadAsync_Action_RunsAction_WhenNoContext()
    {
        var ran = false;

        await CreateWithoutContext().InvokeOnMainThreadAsync(() => ran = true);

        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_FuncTaskOfT_ReturnsRealResult_WhenNoContext() =>
        (await CreateWithoutContext().InvokeOnMainThreadAsync(() => Task.FromResult(7))).ShouldBe(7);

    [Fact]
    public async Task InvokeOnMainThreadAsync_FuncTask_RunsAction_WhenNoContext()
    {
        var ran = false;

        await CreateWithoutContext().InvokeOnMainThreadAsync(() =>
        {
            ran = true;

            return Task.CompletedTask;
        });

        ran.ShouldBeTrue();
    }

    [Fact]
    public void BeginInvokeOnMainThread_PostsThroughContext_WhenPresent()
    {
        var context = new RecordingContext();
        var ran = false;

        CreateWith(context).BeginInvokeOnMainThread(() => ran = true);

        context.Posts.ShouldBe(1);
        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_Func_SendsThroughContext_WhenPresent()
    {
        var context = new RecordingContext();

        (await CreateWith(context).InvokeOnMainThreadAsync(() => 42)).ShouldBe(42);

        context.Sends.ShouldBe(1);
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_Action_SendsThroughContext_WhenPresent()
    {
        var context = new RecordingContext();
        var ran = false;

        await CreateWith(context).InvokeOnMainThreadAsync(() => ran = true);

        context.Sends.ShouldBe(1);
        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_FuncTaskOfT_SendsThroughContext_WhenPresent()
    {
        var context = new RecordingContext();

        (await CreateWith(context).InvokeOnMainThreadAsync(() => Task.FromResult(7))).ShouldBe(7);

        context.Sends.ShouldBe(1);
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_FuncTask_SendsThroughContext_WhenPresent()
    {
        var context = new RecordingContext();
        var ran = false;

        await CreateWith(context).InvokeOnMainThreadAsync(() =>
        {
            ran = true;

            return Task.CompletedTask;
        });

        context.Sends.ShouldBe(1);
        ran.ShouldBeTrue();
    }
}
