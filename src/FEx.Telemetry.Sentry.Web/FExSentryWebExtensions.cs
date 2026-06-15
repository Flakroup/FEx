using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace FEx.Telemetry.Sentry.Web;

public static class FExSentryWebExtensions
{
    public static WebApplicationBuilder AddFExSentry(this WebApplicationBuilder builder, string envVarOverride = "SENTRY_DSN")
    {
        var dsnFromEnv = Environment.GetEnvironmentVariable(envVarOverride);
        var dsnFromConfig = builder.Configuration["Sentry:Dsn"];
        var dsn = !string.IsNullOrWhiteSpace(dsnFromEnv) ? dsnFromEnv : dsnFromConfig;
        if (string.IsNullOrWhiteSpace(dsn))
        {
            return builder;
        }

        builder.WebHost.UseSentry(opt =>
        {
            opt.Dsn = dsn;
            opt.Environment = builder.Configuration["Sentry:Environment"] ?? "Production";
            var sampleRateRaw = builder.Configuration["Sentry:TracesSampleRate"];
            if (!string.IsNullOrWhiteSpace(sampleRateRaw)
                && double.TryParse(sampleRateRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
            {
                opt.TracesSampleRate = rate;
            }
        });
        return builder;
    }
}
