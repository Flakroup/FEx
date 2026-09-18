using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

        // Processors rather than BeforeSend: they also run on user feedback, which skips BeforeSend, and they
        // are a list, so an application's own BeforeSend cannot replace the strip.
        ClientAddressProcessor processor = new(opt);
        opt.AddEventProcessor(processor);
        opt.AddTransactionProcessor(processor);
    }

    /// <summary>
    /// Headers a proxy adds to name the caller's address. The SDK withholds the address itself and the
    /// cookies while SendDefaultPii is off, but it forwards every other header as is - measured on
    /// Sentry.AspNetCore 6.6.0, Cf-Connecting-Ip reached the envelope intact. Behind Cloudflare or a load
    /// balancer that is the visitor's IP, on every error and every sampled transaction.
    /// </summary>
    public static readonly IReadOnlyCollection<string> ClientAddressHeaders =
    [
        "Cf-Connecting-Ip", "Cf-Connecting-Ipv6", "Cf-Pseudo-IPv4", "True-Client-Ip", "X-Forwarded-For",
        "X-Original-Forwarded-For", "X-Forwarded", "Forwarded-For", "Forwarded", "X-Real-Ip", "X-Client-Ip",
        "X-Original-For", "X-Cluster-Client-Ip", "Fastly-Client-Ip", "X-Envoy-External-Address",
        "X-Azure-ClientIP",
    ];

    /// <summary>Removes <see cref="ClientAddressHeaders"/>, whatever case the request spelled them in.</summary>
    public static void StripClientAddress(SentryRequest request)
    {
        foreach (var name in request.Headers.Keys
                     .Where(static name => ClientAddressHeaders.Contains(name, StringComparer.OrdinalIgnoreCase))
                     .ToList())
            request.Headers.Remove(name);
    }

    /// <summary>
    /// Strips at the moment an event leaves, reading SendDefaultPii then rather than at startup, so an
    /// application that changes it in code after this wiring still gets what it asked for.
    /// </summary>
    private sealed class ClientAddressProcessor : ISentryEventProcessor, ISentryTransactionProcessor
    {
        private readonly SentryOptions _options;

        public ClientAddressProcessor(SentryOptions options) => _options = options;

        public SentryEvent Process(SentryEvent @event)
        {
            if (!_options.SendDefaultPii)
                StripClientAddress(@event.Request);

            return @event;
        }

        public SentryTransaction Process(SentryTransaction transaction)
        {
            if (!_options.SendDefaultPii)
                StripClientAddress(transaction.Request);

            return transaction;
        }
    }
}
