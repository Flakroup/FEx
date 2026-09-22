using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Telemetry.Sentry.Web.Tests;

/// <summary>
/// Nothing personal may leave in the request the SDK attaches - not the caller's address, not a secret
/// header, not a search term in the query string - all of which the SDK passes through untouched. Driven through a real
/// <see cref="SentryClient" /> and a capturing transport, so what is asserted is the envelope as it would go
/// on the wire - the callbacks this wires are internal to the SDK and have no other way to be observed.
/// </summary>
public sealed class FExSentryWebExtensionsTests
{
    private const string Dsn = "https://examplePublicKey@o0.ingest.sentry.io/0";
    private const string Address = "203.0.113.77";
    private const string Secret = "secret-marker";
    private const string SearchTerm = "Kowalska";
    private const string Body = "body-pesel-marker";
    private const string Cookie = "session=cookie-marker";

    [Theory]
    [InlineData("/_framework/dotnet.runtime.v06hirbjsv.js", true)]
    [InlineData("/css/app.css", true)]
    [InlineData("/changelog.json", true)]
    [InlineData("/api/sales/trips", false)]
    [InlineData("/api/v1.0/trips", false)]
    [InlineData("/", false)]
    [InlineData("/.well-known", false)]
    [InlineData(null, false)]
    public void IsStaticAsset_IsDecidedByTheLastSegmentOnly(string? path, bool expected) =>
        FExSentryWebExtensions.IsStaticAsset(path).ShouldBe(expected);

    [Theory]
    [InlineData("/_framework/System.Net.Http.Json.lx3tims3m7.wasm", 0.0)]
    [InlineData("/api/sales/trips", 0.1)]
    public void WithATracesRate_StaticAssetsAreDropped_AndTheRestKeepsTheRate(string path, double expected)
    {
        SentryAspNetCoreOptions options = new();
        FExSentryWebExtensions.ConfigureOptions(options, Configuration(("Sentry:TracesSampleRate", "0.1")), Dsn);

        // A caller's sentry-trace header arrives as an already-made decision; the sampler must still decide.
        TransactionSamplingContext context = new(
            new TransactionContext("GET " + path, "http.server", isSampled: true),
            new Dictionary<string, object?> { ["__HttpPath"] = path });

        options.TracesSampleRate.ShouldBe(0.1);
        options.TracesSampler.ShouldNotBeNull().Invoke(context).ShouldBe(expected);
    }

    [Theory]
    [InlineData("sentry-trace")]
    [InlineData("baggage")]
    public void ACallersTraceHeader_GetsTheSamplersOwnDraw_NotTheRate(string header)
    {
        SentryAspNetCoreOptions options = new();
        FExSentryWebExtensions.ConfigureOptions(options, Configuration(("Sentry:TracesSampleRate", "0.1")), Dsn);
        DefaultHttpContext http = new();
        http.Request.Headers[header] = "from-the-caller";
        TransactionSamplingContext context = new(
            new TransactionContext("GET /api/sales/trips", "http.server"),
            new Dictionary<string, object?> { ["__HttpPath"] = "/api/sales/trips", ["__HttpContext"] = http });
        var sampler = options.TracesSampler.ShouldNotBeNull();

        var draws = Enumerable.Range(0, 2000).Select(_ => sampler(context)).ToList();

        // A certainty either way leaves the caller's sample_rand nothing to compare against; 2000 draws at 0.1
        // land within seven standard deviations of 200.
        draws.ShouldAllBe(d => d == 0 || d == 1);
        draws.Count(d => d == 1).ShouldBeInRange(100, 300);
    }

    [Fact]
    public async Task ACallersBaggage_CannotForceItsRequestsIntoTheSample()
    {
        CapturingTransport transport = new();
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Sentry:Dsn"] = Dsn, ["Sentry:TracesSampleRate"] = "0.1",
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddFExSentry("FEX_TESTS_NO_SUCH_VARIABLE");
        builder.Services.PostConfigure<SentryAspNetCoreOptions>(o =>
        {
            o.Transport = transport;
            o.AutoSessionTracking = false;
        });
        await using var app = builder.Build();
        app.MapGet("/api/ping", () => "pong");
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
        for (var i = 0; i < 200; i++)
        {
            var traceId = Guid.NewGuid().ToString("N");
            using HttpRequestMessage request = new(HttpMethod.Get, "/api/ping");
            request.Headers.Add("sentry-trace", $"{traceId}-1111111111111111-1");
            request.Headers.Add(
                "baggage", $"sentry-trace_id={traceId},sentry-public_key=x,sentry-sample_rate=1,sentry-sample_rand=0");
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
        }

        await app.Services.GetRequiredService<IHub>().FlushAsync(TimeSpan.FromSeconds(5));
        await app.StopAsync(TestContext.Current.CancellationToken);

        // Before the sampler drew for itself, all 200 were traced; at 0.1 the expectation is 20.
        transport.Payloads.Count(p => p.Contains("\"type\":\"transaction\"", StringComparison.Ordinal))
                 .ShouldBeInRange(1, 50);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    public void WithoutAPositiveTracesRate_NoSamplerSwitchesTracingOn(string? rate)
    {
        SentryAspNetCoreOptions options = new();
        FExSentryWebExtensions.ConfigureOptions(
            options, rate is null ? Configuration() : Configuration(("Sentry:TracesSampleRate", rate)), Dsn);

        options.TracesSampler.ShouldBeNull();
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(static v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void ScrubRequest_KeepsOnlyTheSafeHeadersInAnyCase_AndDropsTheQuery()
    {
        SentryRequest request = new()
        {
            Url = $"https://example.com/api/journal?text={SearchTerm}", QueryString = $"text={SearchTerm}",
        };
        request.Headers["cf-connecting-ip"] = Address;
        request.Headers["X-Api-Key"] = Secret;
        request.Headers["Cf-Access-Authenticated-User-Email"] = "operator@example.com";
        request.Headers["Referer"] = $"https://example.com/journal?text={SearchTerm}";
        request.Headers["user-agent"] = "agent";
        request.Headers["Host"] = "example.com";

        FExSentryWebExtensions.ScrubRequest(request);

        request.Headers.Keys.ShouldBe(["user-agent", "Host"], ignoreOrder: true);
        request.QueryString.ShouldBeNull();
        request.Url.ShouldBe("https://example.com/api/journal");
    }

    [Fact]
    public async Task AnError_LeavesWithoutPersonalRequestData()
    {
        var sent = await CaptureAsync(sendDefaultPii: false,
            static (client, request, _) => client.CaptureEvent(new SentryEvent { Request = request }));

        sent.ShouldContain("agent-marker");
        ShouldCarryNothingPersonal(sent);
    }

    [Fact]
    public async Task ATransaction_LeavesWithoutPersonalRequestData()
    {
        var sent = await CaptureAsync(sendDefaultPii: false,
            static (client, request, _) => client.CaptureTransaction(
                new SentryTransaction("GET /", "http.server") { Request = request }));

        sent.ShouldContain("agent-marker");
        ShouldCarryNothingPersonal(sent);
    }

    /// <summary>Feedback skips BeforeSend but not the event processors - which is why the scrub is one.</summary>
    [Fact]
    public async Task UserFeedback_LeavesWithoutPersonalRequestData()
    {
        var sent = await CaptureAsync(sendDefaultPii: false,
            static (client, request, options) =>
            {
                // A hub builds its scope from the same options the client runs with; that is where the
                // processors live.
                Scope scope = new(options);
                scope.Request.Url = request.Url;
                scope.Request.QueryString = request.QueryString;
                scope.Request.Data = request.Data;
                scope.Request.Cookies = request.Cookies;
                foreach (var header in request.Headers)
                    scope.Request.Headers[header.Key] = header.Value;
                client.CaptureFeedback(new SentryFeedback("slow page"), out _, scope);
            });

        sent.ShouldContain("agent-marker");
        ShouldCarryNothingPersonal(sent);
    }

    /// <summary>An application that opts in to personal data gets what it asked for, headers and query included.</summary>
    [Fact]
    public async Task WithDefaultPersonalDataOn_TheRequestIsKept()
    {
        var sent = await CaptureAsync(sendDefaultPii: true,
            static (client, request, _) => client.CaptureEvent(new SentryEvent { Request = request }));

        sent.ShouldContain(Address);
        sent.ShouldContain(Secret);
        sent.ShouldContain(SearchTerm);
        sent.ShouldContain(Body);
        sent.ShouldContain(Cookie);
    }

    private static void ShouldCarryNothingPersonal(string sent)
    {
        sent.ShouldNotContain(Address);
        sent.ShouldNotContain(Secret);
        sent.ShouldNotContain(SearchTerm);
        sent.ShouldNotContain(Body);
        sent.ShouldNotContain(Cookie);
    }

    private static async Task<string> CaptureAsync(bool sendDefaultPii, Action<SentryClient, SentryRequest, SentryOptions> capture)
    {
        CapturingTransport transport = new();
        SentryAspNetCoreOptions options = new();
        FExSentryWebExtensions.ConfigureOptions(options, new ConfigurationBuilder().Build(), Dsn);
        // Set after the mapping on purpose: the scrub honours the value that holds when an event is sent, so
        // an application changing it in code after the wiring is not ignored.
        options.SendDefaultPii = sendDefaultPii;
        options.TracesSampleRate = 1.0;
        options.Transport = transport;
        options.AutoSessionTracking = false;

        SentryRequest request = new()
        {
            Url = "https://example.com/api/journal", QueryString = $"text={SearchTerm}", Data = Body, Cookies = Cookie,
        };
        request.Headers["Cf-Connecting-Ip"] = Address;
        request.Headers["X-Forwarded-For"] = Address;
        request.Headers["X-Api-Key"] = Secret;
        request.Headers["User-Agent"] = "agent-marker";

        using (SentryClient client = new(options))
        {
            capture(client, request, options);
            await client.FlushAsync(TimeSpan.FromSeconds(5));
        }

        return string.Join('\n', transport.Payloads);
    }

    private sealed class CapturingTransport : ITransport
    {
        private readonly ConcurrentQueue<string> _payloads = new();

        public IReadOnlyList<string> Payloads => _payloads.ToList();

        public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            using MemoryStream stream = new();
            await envelope.SerializeAsync(stream, null, cancellationToken).ConfigureAwait(false);
            _payloads.Enqueue(Encoding.UTF8.GetString(stream.ToArray()));
        }
    }
}
