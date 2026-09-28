using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FEx.Logging.Web.Tests;

/// <summary>
/// Both <see cref="FExStaticLogger" /> and Serilog's own <see cref="Log.Logger" /> are process-wide static
/// state; a sibling class swapping either concurrently would clobber this class's setup mid-test - the same
/// hazard <c>FEx.Building.Tests.GlobalLoggerCollection</c> documents. One collection runs them one at a time.
/// </summary>
[CollectionDefinition(Name)]
public sealed class FExStaticLoggerCollection
{
    public const string Name = "FExStaticLogger";
}

/// <summary>
/// Before this fix (Flakroup/FEx#156), <c>AddFExSerilog</c> wired ASP.NET Core's own logging pipeline to
/// Serilog but never touched <see cref="FExStaticLogger" />, which stayed on its default <c>FExDebugLogger</c> -
/// a logger whose write path is compiled out of a Release build. Anything a feature logged through the static
/// logger in a host wired only through this extension reached no sink at all.
/// </summary>
[Collection(FExStaticLoggerCollection.Name)]
public sealed class FExSerilogWebExtensionsTests
{
    // UseSerilog itself replaces Log.Logger at host-build time (preserveStaticLogger defaults to false), so
    // the capturing sink is installed AFTER Build() - simulating the host once it is up and running. What this
    // pins is FExStaticLogger's wiring: FExSerilogLogger dispatches to whatever Log.Logger is at call time
    // rather than capturing a reference at construction, so it reaches this sink even though it was installed
    // after AddFExSerilog() ran.
    [Fact]
    public void AddFExSerilog_PointsFExStaticLoggerAtSerilogsAmbientLogger()
    {
        var previousStaticLogger = FExStaticLogger.Instance;
        var previousAmbientLogger = Log.Logger;

        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.AddFExSerilog();
            using var app = builder.Build();

            CapturingSink sink = new();
            Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(sink).CreateLogger();

            FExStaticLogger.Warning("reached-via-fex-static-logger");

            sink.Messages.ShouldContain(m => m.Contains("reached-via-fex-static-logger", StringComparison.Ordinal));
        }
        finally
        {
            Log.Logger = previousAmbientLogger;
            _ = new FExStaticLogger(previousStaticLogger);
        }
    }

    [Fact]
    public void AddFExSerilog_StillConfiguresTheHostsMicrosoftExtensionsLoggingPipeline()
    {
        var previousStaticLogger = FExStaticLogger.Instance;
        var previousAmbientLogger = Log.Logger;

        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.AddFExSerilog();
            using var app = builder.Build();

            // Unchanged existing behaviour: the host's ILoggerFactory is still Serilog-backed - not just "some
            // ILoggerFactory", which WebApplication.CreateBuilder() would supply on its own and this test would
            // then pass even with the UseSerilog call removed.
            app.Services.GetRequiredService<ILoggerFactory>().ShouldBeOfType<SerilogLoggerFactory>();
        }
        finally
        {
            Log.Logger = previousAmbientLogger;
            _ = new FExStaticLogger(previousStaticLogger);
        }
    }

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly List<string> _messages = [];

        public IReadOnlyList<string> Messages => _messages;

        public void Emit(LogEvent logEvent) => _messages.Add(logEvent.RenderMessage());
    }
}
