using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
#pragma warning disable VSTHRD003 // the TaskCompletionSources are completed by the sibling flows started in the same test
#pragma warning disable IDISP004, IDISP016, IDISP017 // EndScope/Dispose closing the scopes is the behaviour under test
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Logging.Tests;

/// <summary>
/// <see cref="Log.Logger" /> is process-wide static state, so these run in the same collection as the other
/// tests that swap it, and each test restores it.
/// </summary>
[Collection(FExStaticLoggerCollection.Name)]
public sealed class FExSerilogLoggerTests
{
    // Flakroup/FEx#63: a second BeginScope used to dispose the first one while the caller still held it.
    [Fact]
    public void BeginScope_Nested_KeepsOuterScopeActiveUntilItIsDisposed()
    {
        var previous = Log.Logger;
        CapturingSink sink = new();

        try
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
            using var logger = new FExSerilogLogger();

            using (logger.BeginScope("outer"))
            {
                using (logger.BeginScope("inner"))
                    logger.Information("in-inner");

                logger.Information("back-in-outer");
            }

            logger.Information("after-all");

            sink.ScopeOf("in-inner").ShouldBe("inner");
            sink.ScopeOf("back-in-outer").ShouldBe("outer");
            sink.ScopeOf("after-all").ShouldBeNull();
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    // Flakroup/FEx#160: the exception overloads raised ErrorLogged without the exception.
    [Fact]
    public void ErrorAndCritical_WithException_RaiseErrorLoggedCarryingTheException()
    {
        var previous = Log.Logger;

        try
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Sink(new CapturingSink()).CreateLogger();
            using var logger = new FExSerilogLogger();
            List<FExErrorEventArgs> raised = [];
            logger.ErrorLogged += (_, e) => raised.Add(e);
            InvalidOperationException error = new("boom");
            InvalidOperationException critical = new("fatal");

            logger.Error(error, "e");
            logger.Critical(critical, "c");

            raised.Select(e => e.Exception).ShouldBe([error, critical]);
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    // Flakroup/FEx#159: with Log.Logger on Serilog's SilentLogger (before the host is built / after it is
    // disposed) messages used to vanish. The fallback is stderr, which - unlike Debug.WriteLine - survives Release.
    [Fact]
    public void Write_WhileSerilogIsSilent_FallsBackToStandardError()
    {
        var previous = Log.Logger;
        var previousError = Console.Error;
        using StringWriter output = new();

        try
        {
            Log.CloseAndFlush(); // resets Log.Logger to SilentLogger
            Console.SetError(output);
            using var logger = new FExSerilogLogger();

            logger.Warning("reached-the-fallback");
            logger.Error(new InvalidOperationException("x"), "error-reached-the-fallback");

            output.ToString().ShouldContain("reached-the-fallback");
            output.ToString().ShouldContain("error-reached-the-fallback");
            output.ToString().ShouldContain("InvalidOperationException");
        }
        finally
        {
            Console.SetError(previousError);
            Log.Logger = previous;
        }
    }

    // Flakroup/FEx#63 follow-up: scopes live per async flow, so concurrent flows neither throw nor interfere.
    [Fact]
    public async Task BeginScope_ConcurrentFlows_NeitherThrowNorLeakIntoEachOther()
    {
        var previous = Log.Logger;
        CapturingSink sink = new();

        try
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
            using var logger = new FExSerilogLogger();

            await Task.WhenAll(Enumerable.Range(0, 8).Select(t => Task.Run(() =>
            {
                for (var i = 0; i < 500; i++)
                {
                    using (logger.BeginScope($"flow-{t}"))
                        logger.Information($"msg-{t}-{i}");
                }
            }, TestContext.Current.CancellationToken)));

            sink.Count.ShouldBe(8 * 500);
            sink.Events.ShouldAllBe(e => ScopeOf(e) == "flow-" + e.RenderMessage().Split('-')[1]);
            logger.Information("after");
            ScopeOf(sink.Events.Single(e => e.RenderMessage() == "after")).ShouldBeNull();
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    [Fact]
    public async Task EndScope_ClosesOnlyTheCurrentFlowsScope()
    {
        var previous = Log.Logger;
        CapturingSink sink = new();

        try
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
            using var logger = new FExSerilogLogger();
            TaskCompletionSource aBegan = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource bBegan = new(TaskCreationOptions.RunContinuationsAsynchronously);
            LoggerState stateA = new();
            LoggerState stateB = new();
            TaskCompletionSource aEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);

            var flowA = Task.Run(async () =>
            {
                logger.BeginLabeledScope(stateA);
                aBegan.SetResult();
                await bBegan.Task;
                logger.EndScope(); // must close A, not B (the newest scope of any flow)
                logger.AddOrUpdateLabel("a-probe", 1); // A has no scope left: reaches neither state
                logger.Information("a-after-end");
                aEnded.SetResult();
            }, TestContext.Current.CancellationToken);

            var flowB = Task.Run(async () =>
            {
                await aBegan.Task;
                logger.BeginLabeledScope(stateB);
                bBegan.SetResult();
                await aEnded.Task;
                logger.AddOrUpdateLabel("b-label", 1); // B's scope must still be B's current one
                logger.Information("b-after-a-ended");
                logger.EndScope();
                logger.Information("b-after-end");
            }, TestContext.Current.CancellationToken);

            await Task.WhenAll(flowA, flowB);

            stateA.ShouldBeEmpty();
            stateB.Keys.ShouldBe(["b-label"]);
            ScopeOf(sink.Single("a-after-end")).ShouldBeNull();
            ScopeOf(sink.Single("b-after-end")).ShouldBeNull();
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    [Fact]
    public void EndScope_ClosesTheInnermostScopeAndRestoresTheOuterOne()
    {
        var previous = Log.Logger;
        CapturingSink sink = new();

        try
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
            using var logger = new FExSerilogLogger();

            logger.BeginScope("outer");
            logger.BeginScope("inner");
            logger.EndScope();
            logger.Information("after-inner-end");
            logger.EndScope();
            logger.Information("after-outer-end");
            logger.EndScope(); // nothing left: must not throw

            ScopeOf(sink.Single("after-inner-end")).ShouldBe("outer");
            ScopeOf(sink.Single("after-outer-end")).ShouldBeNull();
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    [Fact]
    public void Dispose_ClosesAllOpenScopesOfTheFlow()
    {
        var previous = Log.Logger;
        CapturingSink sink = new();

        try
        {
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
            var logger = new FExSerilogLogger();

            logger.BeginScope("outer");
            logger.BeginScope("inner");
            logger.Dispose();
            logger.Information("after-dispose");

            ScopeOf(sink.Single("after-dispose")).ShouldBeNull();
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    [Fact]
    public void Labels_AreAppliedToTheInnermostScopeAndRestoredWithTheOuterOne()
    {
        var previous = Log.Logger;

        try
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Sink(new CapturingSink()).CreateLogger();
            using var logger = new FExSerilogLogger();
            LoggerState outer = new(("k", "outer-value"));
            LoggerState inner = new();

            logger.AddOrUpdateLabel("ignored", 0); // no scope: must not throw

            using (logger.BeginLabeledScope(outer))
            {
                using (logger.BeginLabeledScope(inner))
                {
                    logger.AddOrUpdateLabel("k", "inner-value");
                    inner["k"].ShouldBe("inner-value");
                    outer["k"].ShouldBe("outer-value");
                }

                logger.AddOrUpdateLabel("k", "changed");
                outer["k"].ShouldBe("changed");
                logger.RemoveLabel("k");
                outer.ContainsKey("k").ShouldBeFalse();
                inner["k"].ShouldBe("inner-value");
            }
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    // A scope disposed from another flow (created in Task.Run, disposed by the parent) stays current in its own
    // flow: EndScope must still pop it and Dispose must terminate.
    [Fact]
    public async Task ScopeDisposedFromAnotherFlow_DoesNotWedgeEndScopeOrDispose()
    {
        var previous = Log.Logger;

        try
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Sink(new CapturingSink()).CreateLogger();
            using var logger = new FExSerilogLogger();
            LoggerState endScopeState = new();
            LoggerState disposeState = new();

            var endScopeFlow = await RunWithScopeDisposedByParent(logger, endScopeState, _ =>
            {
                logger.EndScope();
                logger.AddOrUpdateLabel("probe", 1); // the scope was popped: reaches no state
            });
            var disposeFlow = await RunWithScopeDisposedByParent(logger, disposeState, _ => logger.Dispose());

            await Task.WhenAll(endScopeFlow, disposeFlow).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            endScopeState.ShouldBeEmpty();
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    private static async Task<Task> RunWithScopeDisposedByParent(
        FExSerilogLogger logger, LoggerState state, Action<FExSerilogLogger> inFlowAfterwards)
    {
        TaskCompletionSource<IDisposable> began = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource disposedByParent = new(TaskCreationOptions.RunContinuationsAsynchronously);

        var flow = Task.Run(async () =>
        {
            began.SetResult(logger.BeginLabeledScope(state));
            await disposedByParent.Task;
            inFlowAfterwards(logger);
        }, TestContext.Current.CancellationToken);

        (await began.Task).Dispose();
        disposedByParent.SetResult();

        return flow;
    }

    private static string? ScopeOf(LogEvent e) =>
        e.Properties.TryGetValue("Scope", out var value) ? ((ScalarValue)value).Value?.ToString() : null;

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly ConcurrentQueue<LogEvent> _events = new();

        public IReadOnlyCollection<LogEvent> Events => _events;

        public int Count => _events.Count;

        public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

        public LogEvent Single(string message) => _events.Single(x => x.RenderMessage() == message);

        public string? ScopeOf(string message) => FExSerilogLoggerTests.ScopeOf(Single(message));
    }
}
