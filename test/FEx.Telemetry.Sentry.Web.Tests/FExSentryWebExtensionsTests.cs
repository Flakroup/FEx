using Microsoft.Extensions.Configuration;
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
    }

    private static void ShouldCarryNothingPersonal(string sent)
    {
        sent.ShouldNotContain(Address);
        sent.ShouldNotContain(Secret);
        sent.ShouldNotContain(SearchTerm);
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

        SentryRequest request = new() { Url = "https://example.com/api/journal", QueryString = $"text={SearchTerm}" };
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
