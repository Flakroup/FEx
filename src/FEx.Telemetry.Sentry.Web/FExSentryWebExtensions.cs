using FEx.Agnostics.Abstractions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    private const string SampleRateKey = "Sentry:TracesSampleRate";

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

        // Sentry's own options setup binds the "Sentry" section before the UseSentry callback runs, so an unusable
        // rate has to be neutralised in configuration first or the binder throws (or applies NaN) during Build().
        NeutraliseUnusableSampleRate(builder.Configuration);

        IConfiguration configuration = builder.Configuration;
        // Registered ahead of UseSentry, so it runs ahead of Sentry's own middleware.
        builder.Services.AddTransient<IStartupFilter, CallerTraceHeaderStripper>();
        builder.WebHost.UseSentry(opt => ConfigureOptions(opt, configuration, dsn));

        return builder;
    }

    // The SDK's setter throws for anything outside [0, 1] (which also covers the infinities), and NaN slips past
    // its range check, so only a parsable number inside the range is usable.
    private static bool TryParseSampleRate(string raw, out double rate) =>
        double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out rate) && rate is >= 0 and <= 1;

    private static void NeutraliseUnusableSampleRate(ConfigurationManager configuration)
    {
        var raw = configuration[SampleRateKey];

        if (string.IsNullOrWhiteSpace(raw) || TryParseSampleRate(raw, out _))
            return;

        WarnUnusableSampleRate(raw);
        // A later source wins; the null leaves the key unset for Sentry's binder, so the SDK default stays.
        configuration.AddInMemoryCollection(new Dictionary<string, string?> { [SampleRateKey] = null });
    }

    private static void WarnUnusableSampleRate(string raw)
    {
        // Rejected silently otherwise - most plausibly a locale-formatted decimal such as "0,5" (this parses
        // InvariantCulture) or a percentage such as "10" - and the SDK default stays in effect with no other signal.
        // The raw value is deploy configuration, not caller input, but a newline in it would still forge
        // a second line in a plain-text sink - stripped before it reaches the message.
        var sanitizedRaw = raw.Replace("\r", string.Empty).Replace("\n", string.Empty);
        FExStaticLogger.Warning(
            $"{SampleRateKey} value '{sanitizedRaw}' is not a number between 0 and 1; " +
            "keeping the Sentry SDK's default sample rate.");
    }

    // UseSentry defers invoking its callback to Sentry's own host startup, so it is not exercised by a
    // plain AddFExSentry() call in a test - split out so the option-mapping logic is directly testable.
    public static void ConfigureOptions(SentryAspNetCoreOptions opt, IConfiguration configuration, string dsn)
    {
        opt.Dsn = dsn;
        opt.Environment = configuration["Sentry:Environment"] ?? "Production";
        var sampleRateRaw = configuration[SampleRateKey];

        if (!string.IsNullOrWhiteSpace(sampleRateRaw))
        {
            if (TryParseSampleRate(sampleRateRaw, out var rate))
            {
                opt.TracesSampleRate = rate;
                // Only where tracing is on: a sampler alone switches performance monitoring on, even at a rate of 0.
                // It never returns null, since null defers to an incoming sentry-trace header and lets any anonymous
                // caller force every request of theirs into the sampled budget.
                if (rate > 0)
                    opt.TracesSampler = context => IsStaticAsset(context.TryGetHttpPath()) ? 0 : rate;
            }
            else
            {
                WarnUnusableSampleRate(sampleRateRaw);
            }
        }

        // Processors rather than BeforeSend: they also run on user feedback, which skips BeforeSend, and they
        // are a list, so an application's own BeforeSend cannot replace the scrub.
        RequestScrubber scrubber = new(opt);
        opt.AddEventProcessor(scrubber);
        opt.AddTransactionProcessor(scrubber);
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

    /// <summary>
    /// Sentry compares its sample rate with a sample_rand it takes from the caller's baggage, or derives from the
    /// caller's trace id, so a caller sending either header picks whether it is traced - baggage sample_rand=0
    /// traced 100 requests out of 100 at a rate of 0.1, and a sampler's own draw still left it the backpressure
    /// throttle. With the headers gone every request starts its own trace. The cost is that a trace from an
    /// upstream service no longer continues here, which no caller of these hosts needs today.
    /// </summary>
    private sealed class CallerTraceHeaderStripper : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Request.Headers.Remove("sentry-trace");
                context.Request.Headers.Remove("baggage");
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
