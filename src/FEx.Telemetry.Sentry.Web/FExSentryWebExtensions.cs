using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        {
            opt.TracesSampleRate = rate;
            // Only where tracing is on: a sampler alone switches performance monitoring on, even at a rate of 0.
            // It never returns null, since null defers to an incoming sentry-trace header and lets any anonymous
            // caller force every request of theirs into the sampled budget.
            if (rate > 0)
                opt.TracesSampler = context => Sample(context, rate);
        }

        // Processors rather than BeforeSend: they also run on user feedback, which skips BeforeSend, and they
        // are a list, so an application's own BeforeSend cannot replace the scrub.
        RequestScrubber scrubber = new(opt);
        opt.AddEventProcessor(scrubber);
        opt.AddTransactionProcessor(scrubber);
    }

    // The SDK compares the returned rate with a sample_rand it takes from the caller's baggage, or derives from
    // the caller's trace id, so a caller sending either header picks the outcome - baggage sample_rand=0 traced
    // 100 requests out of 100 at a rate of 0.1. Drawing here and answering with a certainty takes that pick away.
    // A request without those headers keeps the rate itself, which Sentry needs to extrapolate from the sample.
    private static double Sample(TransactionSamplingContext context, double rate)
    {
        if (IsStaticAsset(context.TryGetHttpPath()))
            return 0;

        if (context.TryGetHttpContext()?.Request.Headers is not { } headers
            || !(headers.ContainsKey("sentry-trace") || headers.ContainsKey("baggage")))
            return rate;

        return Random.Shared.NextDouble() < rate ? 1 : 0;
    }

    /// <summary>
    /// A request for a file - its last path segment carries an extension, as every framework asset, stylesheet,
    /// script and manifest does and no API route does. Measured on a Blazor WebAssembly host at a flat 0.1 rate:
    /// every sampled transaction in the first hour was such a file, each under 5 ms, and none was an API call.
    /// </summary>
    public static bool IsStaticAsset(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var lastSegment = path[(path.LastIndexOf('/') + 1)..];
        return lastSegment.IndexOf('.', StringComparison.Ordinal) > 0;
    }

    /// <summary>
    /// The only request headers that leave while SendDefaultPii is off. The SDK withholds the caller's address
    /// and the cookies by itself but forwards every other header as is - measured on Sentry.AspNetCore 6.6.0,
    /// Cf-Connecting-Ip, an application's own API key header and Cloudflare Access's user e-mail all reached
    /// the envelope intact. A list of what is safe holds where a list of what is not always misses the next
    /// proxy's or the next application's header.
    /// </summary>
    public static readonly IReadOnlyCollection<string> SafeRequestHeaders =
    [
        "Accept", "Accept-Encoding", "Accept-Language", "Content-Length", "Content-Type", "Host", "User-Agent",
    ];

    /// <summary>
    /// Keeps only <see cref="SafeRequestHeaders" />, whatever case the request spelled them in, and drops the
    /// query string and the body - the SDK sends the query whatever SendDefaultPii says, and the body whenever
    /// MaxRequestBodySize is configured, and a search term, a link token or a form is as personal as a header.
    /// Covers the incoming request only: an outgoing HttpClient call's span and breadcrumb still carry its
    /// full URL, and a token in the path stays in Url.
    /// </summary>
    public static void ScrubRequest(SentryRequest request)
    {
        foreach (var name in request.Headers.Keys
                     .Where(static name => !SafeRequestHeaders.Contains(name, StringComparer.OrdinalIgnoreCase))
                     .ToList())
            request.Headers.Remove(name);

        request.QueryString = null;
        request.Data = null;
        request.Cookies = null;
        // The SDK builds Url without the query today; this holds if it ever stops.
        var query = request.Url?.IndexOf('?', StringComparison.Ordinal) ?? -1;
        if (query >= 0)
            request.Url = request.Url![..query];
    }

    /// <summary>
    /// Scrubs at the moment an event leaves, reading SendDefaultPii then rather than at startup, so an
    /// application that changes it in code after this wiring still gets what it asked for.
    /// </summary>
    private sealed class RequestScrubber : ISentryEventProcessor, ISentryTransactionProcessor
    {
        private readonly SentryOptions _options;

        public RequestScrubber(SentryOptions options) => _options = options;

        public SentryEvent Process(SentryEvent @event)
        {
            if (!_options.SendDefaultPii)
                ScrubRequest(@event.Request);

            return @event;
        }

        public SentryTransaction Process(SentryTransaction transaction)
        {
            if (!_options.SendDefaultPii)
                ScrubRequest(transaction.Request);

            return transaction;
        }
    }
}
