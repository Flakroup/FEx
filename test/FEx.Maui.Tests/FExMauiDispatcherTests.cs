using FEx.Core.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Dispatching;
using NSubstitute;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Maui.Tests;

/// <summary>
/// The plain <c>net10.0</c> MAUI assemblies have no platform dispatcher (<c>Dispatcher.GetForCurrentThread()</c> is
/// <see langword="null" /> there), so each test installs a provider that hands out a fake <see cref="IDispatcher" />.
/// The provider is process-global, hence the serial collection.
/// </summary>
[Collection(MauiDispatcherCollection.Name)]
public sealed class FExMauiDispatcherTests : IDisposable
{
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
    private readonly IDispatcherProvider _provider = Substitute.For<IDispatcherProvider>();
    private readonly IMainThreadContextProvider _mainThread = Substitute.For<IMainThreadContextProvider>();
    private readonly IDeadlockMonitor _deadlockMonitor = Substitute.For<IDeadlockMonitor>();

    public FExMauiDispatcherTests()
    {
        // The real dispatcher runs the action on its thread; the fake runs it inline.
        _dispatcher.Dispatch(Arg.Any<Action>())
            .Returns(call =>
            {
                call.Arg<Action>()();

                return true;
            });
        _provider.GetForCurrentThread().Returns(_dispatcher);
        _ = DispatcherProvider.SetCurrent(_provider);
    }

    public void Dispose() => _ = DispatcherProvider.SetCurrent(null);

    private FExMauiDispatcher CreateSut() =>
        new(Substitute.For<ILogger>(), _mainThread, _deadlockMonitor, Substitute.For<IStackTraceProvider>());

    [Fact]
    public void Constructor_NoDispatcherOnTheThread_Throws()
    {
        _provider.GetForCurrentThread().Returns((IDispatcher?)null);

        var error = Should.Throw<ArgumentNullException>(CreateSut);

        error.ParamName.ShouldBe(nameof(Dispatcher));
    }

    [Fact]
    public void Constructor_ResolvesTheDispatcherOfTheConstructingThread()
    {
        _ = CreateSut();

        _provider.Received(1).GetForCurrentThread();
    }

    [Fact]
    public void BeginInvokeOnMainThread_HandsTheActionToTheDispatcher()
    {
        var sut = CreateSut();
        var ran = false;
        void Action() => ran = true;
        _dispatcher.ClearReceivedCalls();

        sut.BeginInvokeOnMainThread(Action, new object());

        ran.ShouldBeTrue();
        _dispatcher.Received(1).Dispatch(Arg.Any<Action>());
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_Action_RunsItThroughTheDispatcher()
    {
        var sut = CreateSut();
        var ran = 0;

        await sut.InvokeOnMainThreadAsync(() => ran++);

        ran.ShouldBe(1);
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_ActionThatThrows_FaultsTheTask()
    {
        var sut = CreateSut();

        var error = await Record.ExceptionAsync(() =>
            sut.InvokeOnMainThreadAsync(() => throw new InvalidOperationException("boom")));

        error.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("boom");
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_Func_ReturnsTheResult()
    {
        var sut = CreateSut();

        var result = await sut.InvokeOnMainThreadAsync(() => 42);

        result.ShouldBe(42);
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_AsyncFuncWithResult_ReturnsTheAwaitedResult()
    {
        var sut = CreateSut();

        var result = await sut.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();

            return "done";
        });

        result.ShouldBe("done");
    }

    [Fact]
    public async Task InvokeOnMainThreadAsync_AsyncFunc_WaitsForTheWork()
    {
        var sut = CreateSut();
        var finished = false;

        await sut.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();
            finished = true;
        });

        finished.ShouldBeTrue();
    }

    [Fact]
    public void CheckAccess_ReportsWhatTheDispatcherSaysAboutDispatchRequired()
    {
        // Pins today's behaviour: the value is IDispatcher.IsDispatchRequired itself, not its negation.
        var sut = CreateSut();

        _dispatcher.IsDispatchRequired.Returns(true);
        sut.CheckAccess().ShouldBeTrue();

        _dispatcher.IsDispatchRequired.Returns(false);
        sut.CheckAccess(new object()).ShouldBeFalse();
    }

    [Fact]
    public void EnableCollectionSynchronization_DoesNothing()
    {
        var sut = CreateSut();
        var callbackCalls = 0;

        Should.NotThrow(() => sut.EnableCollectionSynchronization(new[] { 1, 2 },
            new object(),
            (_, _, _, _) => callbackCalls++));

        callbackCalls.ShouldBe(0);
    }

    [Fact]
    public void SendInContext_DeadlockMonitoringOffByDefault_SendsThroughTheMainContextWithoutTheMonitor()
    {
        var context = new RecordingContext();
        _mainThread.Context.Returns(context);
        var sut = CreateSut();
        var ran = false;

        sut.SendInContext(() => ran = true, new object(), true);

        ran.ShouldBeTrue();
        context.Sends.ShouldBe(1);
        _deadlockMonitor.DidNotReceiveWithAnyArgs().Execute(default!, default, default);
    }

    [Fact]
    public void SendInContext_DeadlockMonitoringEnabled_RunsThroughTheMonitorWithTheTimeout()
    {
        var context = new RecordingContext();
        _mainThread.Context.Returns(context);
        var sut = new FExMauiDispatcher(Substitute.For<ILogger>(),
            _mainThread,
            _deadlockMonitor,
            Substitute.For<IStackTraceProvider>(),
            true);

        sut.SendInContext(() => { }, new object(), true, 1234);

        _deadlockMonitor.Received(1).Execute(Arg.Any<Action>(), Arg.Any<System.Diagnostics.StackTrace?>(), 1234u);
    }

    private sealed class RecordingContext : SynchronizationContext
    {
        public int Sends { get; private set; }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Sends++;
            d(state);
        }
    }
}
