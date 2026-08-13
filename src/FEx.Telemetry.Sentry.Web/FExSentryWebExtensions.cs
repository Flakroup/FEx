using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Sentry.AspNetCore;
using System;
using System.Globalization;

namespace FEx.Telemetry.Sentry.Web;

public static class FExSentryWebExtensions
{
    public static WebApplicationBuilder AddFExSentry(this WebApplicationBuilder builder,
                                                     string envVarOverride = "SENTRY_DSN")
    {
        var dsnFromEnv = Environment.GetEnvironmentVariable(envVarOverride);
        var dsnFromConfig = builder.Configuration["Sentry:Dsn"];

        var dsn = !string.IsNullOrWhiteSpace(dsnFromEnv)
            ? dsnFromEnv
            : dsnFromConfig;

        if (string.IsNullOrWhiteSpace(dsn))
            return builder;

        IConfiguration configuration = builder.Configuration;
        builder.WebHost.UseSentry(opt => ConfigureOptions(opt, configuration, dsn));

        return builder;
    }

    // UseSentry defers invoking its callback to Sentry's own host startup, so it is not exercised by a
    // plain AddFExSentry() call in a test - split out so the option-mapping logic is directly testable.
    public static void ConfigureOptions(SentryAspNetCoreOptions opt, IConfiguration configuration, string dsn)
    {
        opt.Dsn = dsn;
        opt.Environment = configuration["Sentry:Environment"] ?? "Production";
        var sampleRateRaw = configuration["Sentry:TracesSampleRate"];

        if (!string.IsNullOrWhiteSpace(sampleRateRaw)
            && double.TryParse(sampleRateRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
            opt.TracesSampleRate = rate;
    }
}