using FEx.Agnostics.Abstractions.Flow;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

#if DEBUG
    // Flakroup/FEx#159: with Log.Logger on Serilog's SilentLogger (before the host is built / after it is
    // disposed) messages used to vanish. FExDebugLogger's output is compiled out of Release builds, hence DEBUG.
    [Fact]
    public void Write_WhileSerilogIsSilent_FallsBackToTheDebugLogger()
    {
        var previous = Log.Logger;
        using StringWriter output = new();
        using TextWriterTraceListener listener = new(output);

        try
        {
            Log.CloseAndFlush(); // resets Log.Logger to SilentLogger
            Trace.Listeners.Add(listener);
            using var logger = new FExSerilogLogger();

            logger.Warning("reached-the-fallback");
            logger.Error(new InvalidOperationException("x"), "error-reached-the-fallback");
            listener.Flush();

            output.ToString().ShouldContain("reached-the-fallback");
            output.ToString().ShouldContain("error-reached-the-fallback");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
            Log.Logger = previous;
        }
    }
#endif

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly List<LogEvent> _events = [];

        public void Emit(LogEvent logEvent) => _events.Add(logEvent);

        public string? ScopeOf(string message)
        {
            var e = _events.Single(x => x.RenderMessage() == message);

            return e.Properties.TryGetValue("Scope", out var value) ? ((ScalarValue)value).Value?.ToString() : null;
        }
    }
}
