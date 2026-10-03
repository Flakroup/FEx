using FEx.Agnostics.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;

namespace FEx.Agnostics.Abstractions.Extensions.Web;

/// <summary>
/// Hands out long-lived <see cref="HttpClient" /> instances. Handler-level settings (credentials, cookies, proxy,
/// certificate validation) live on the handler, so params that carry them get a client from a small bounded cache keyed
/// by those <em>values</em> - a caller that builds a new <see cref="WebRequestParams" /> per call still reuses the same
/// client and connection pool. Everything else shares one default client. Per-request settings (method, headers,
/// timeout) are applied to the request message instead.
/// </summary>
internal static class HttpClientProvider
{
    internal const int MaxCachedClients = 32;

    private static readonly Lazy<HttpClient> Shared = new(() => Create(null), LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly object Gate = new();
    private static readonly Dictionary<ClientKey, Entry> Cache = [];
    private static long _clock;

    public static HttpClient Get(WebRequestParams? pars)
    {
        if (pars is null
            || !NeedsOwnHandler(pars))
            return Shared.Value;

        var key = new ClientKey(GetCredentialsKey(pars.Credentials),
            pars.Cookies,
            pars.Proxy,
            pars.IsProxyNull,
            pars.ServerCertificateValidationCallback);

        lock (Gate)
        {
            if (!Cache.TryGetValue(key, out var entry))
            {
                if (Cache.Count >= MaxCachedClients)
                    // The evicted client is not disposed: a request may still be using it. It is collected once idle.
                    Cache.Remove(Cache.OrderBy(x => x.Value.LastUsed).First().Key);

                entry = new(Create(pars));
                Cache[key] = entry;
            }

            entry.LastUsed = ++_clock;

            return entry.Client;
        }
    }

    internal static int CachedClientsCount
    {
        get
        {
            lock (Gate)
                return Cache.Count;
        }
    }

    private static bool NeedsOwnHandler(WebRequestParams pars) =>
        pars.Credentials is not null
        || pars.Cookies is not null
        || pars.Proxy is not null
        || pars.IsProxyNull
        || pars.ServerCertificateValidationCallback is not null;

    // NetworkCredential has no value equality, so key it by its content; other ICredentials by reference.
    private static object? GetCredentialsKey(ICredentials? credentials) =>
        credentials is NetworkCredential nc
            ? (nc.UserName, nc.Domain, nc.Password)
            : credentials;

    private static HttpClient Create(WebRequestParams? pars)
    {
#pragma warning disable IDISP001 // handler ownership transferred to HttpClient, which lives for the process lifetime
        var handler = pars?.GetHttpClientHandler() ?? new HttpClientHandler();
#pragma warning restore IDISP001

        // Cookies are only kept when the caller supplied a container; otherwise a Set-Cookie of one caller would be
        // replayed to every other caller of the same client.
        handler.UseCookies = pars?.Cookies is not null;

        // Timeouts are applied per request through a CancellationToken, not through the client-wide Timeout.
        return new(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    private readonly record struct ClientKey(object? Credentials,
                                             CookieContainer? Cookies,
                                             IWebProxy? Proxy,
                                             bool NoProxy,
                                             RemoteCertificateValidationCallback? CertificateCallback);

    private sealed class Entry(HttpClient client)
    {
        public HttpClient Client { get; } = client;
        public long LastUsed { get; set; }
    }
}
