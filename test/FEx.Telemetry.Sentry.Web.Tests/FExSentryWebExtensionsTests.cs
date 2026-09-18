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
/// The caller's address must not leave in a header the SDK passes through untouched. Driven through a real
/// <see cref="SentryClient" /> and a capturing transport, so what is asserted is the envelope as it would go
/// on the wire - the callbacks this wires are internal to the SDK and have no other way to be observed.
/// </summary>
public sealed class FExSentryWebExtensionsTests
{
    private const string Dsn = "https://examplePublicKey@o0.ingest.sentry.io/0";
    private const string Address = "203.0.113.77";

    [Fact]
    public void StripClientAddress_RemovesEveryAddressHeaderInAnyCase_AndKeepsTheRest()
    {
        SentryRequest request = new();
        foreach (var name in FExSentryWebExtensions.ClientAddressHeaders)
            request.Headers[name.ToLowerInvariant()] = Address;
        request.Headers["User-Agent"] = "agent";
        request.Headers["Host"] = "example.com";

        FExSentryWebExtensions.StripClientAddress(request);

        request.Headers.Keys.ShouldBe(["User-Agent", "Host"], ignoreOrder: true);
    }

    [Fact]
    public async Task AnError_LeavesWithoutTheCallersAddress()
    {
        var sent = await CaptureAsync(sendDefaultPii: false,
            static (client, request) => client.CaptureEvent(new SentryEvent { Request = request }));

        sent.ShouldContain("agent-marker");
        sent.ShouldNotContain(Address);
    }

    [Fact]
    public async Task ATransaction_LeavesWithoutTheCallersAddress()
    {
        var sent = await CaptureAsync(sendDefaultPii: false,
            static (client, request) => client.CaptureTransaction(
                new SentryTransaction("GET /", "http.server") { Request = request }));

        sent.ShouldContain("agent-marker");
        sent.ShouldNotContain(Address);
    }

    /// <summary>An application that opts in to personal data gets what it asked for, headers included.</summary>
    [Fact]
    public async Task WithDefaultPersonalDataOn_TheAddressIsKept()
    {
        var sent = await CaptureAsync(sendDefaultPii: true,
            static (client, request) => client.CaptureEvent(new SentryEvent { Request = request }));

        sent.ShouldContain(Address);
    }

    private static async Task<string> CaptureAsync(bool sendDefaultPii, Action<SentryClient, SentryRequest> capture)
    {
        CapturingTransport transport = new();
        SentryAspNetCoreOptions options = new();
        FExSentryWebExtensions.ConfigureOptions(options, new ConfigurationBuilder().Build(), Dsn);
        // Set after the mapping on purpose: Sentry binds its configuration section later, and the strip has
        // to honour the value that holds when an event is sent, not the one seen at startup.
        options.SendDefaultPii = sendDefaultPii;
        options.TracesSampleRate = 1.0;
        options.Transport = transport;
        options.AutoSessionTracking = false;

        SentryRequest request = new() { Url = "https://example.com/" };
        request.Headers["Cf-Connecting-Ip"] = Address;
        request.Headers["X-Forwarded-For"] = Address;
        request.Headers["User-Agent"] = "agent-marker";

        using (SentryClient client = new(options))
        {
            capture(client, request);
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
