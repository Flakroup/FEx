using FEx.Agnostics.Abstractions.Logging;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace FEx.Logging.Web;

public static class FExSerilogWebExtensions
{
    public static WebApplicationBuilder AddFExSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });

        // FExStaticLogger otherwise stays on its default FExDebugLogger, whose write path is compiled out of
        // a Release build (Debug.WriteLine is [Conditional("DEBUG")]) - so anything a feature logs through the
        // static logger in a host wired only through this extension reached no sink at all (Flakroup/FEx#156).
        // FExSerilogLogger dispatches to Serilog's ambient Log.Logger on each call rather than capturing it at
        // construction, so this holds regardless of whether UseSerilog above has already run its deferred setup.
#pragma warning disable IDISP005 // FExStaticLogger owns the instance for the app's lifetime - same rationale
                                  // as FExSerilogLogger's own IDISP025 suppression at its class declaration.
        FExStaticLogger.Configure(() => new FExSerilogLogger());
#pragma warning restore IDISP005

        return builder;
    }
}
