#pragma warning disable IDISP001, IDISP004 // clients from the provider are process-lifetime singletons by design, never disposed by callers
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Models;
using Shouldly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Downloader.Tests;

public sealed class HttpClientProviderTests
{
    [Fact]
    public void Get_ParamsWithoutHandlerSettings_ShareOneClient() =>
        HttpClientProvider.Get(new() { Method = "GET", UserAgent = "x" })
            .ShouldBeSameAs(HttpClientProvider.Get(null));

    [Fact]
    public void Get_EqualCredentialsInNewParamsEachCall_ReuseTheSameClient()
    {
        var first = HttpClientProvider.Get(new() { Credentials = new NetworkCredential("user", "pw", "dom") });
        var second = HttpClientProvider.Get(new() { Credentials = new NetworkCredential("user", "pw", "dom") });
        var other = HttpClientProvider.Get(new() { Credentials = new NetworkCredential("user", "other", "dom") });

        second.ShouldBeSameAs(first);
        other.ShouldNotBeSameAs(first);
        first.ShouldNotBeSameAs(HttpClientProvider.Get(null));
    }

    [Fact]
    public void Get_SameCookieContainer_ReusesTheClient()
    {
        var cookies = new CookieContainer();

        HttpClientProvider.Get(new() { Cookies = cookies })
            .ShouldBeSameAs(HttpClientProvider.Get(new() { Cookies = cookies }));
    }

    [Fact]
    public void Get_ManyDistinctSettings_KeepsTheCacheBounded()
    {
        foreach (var i in Enumerable.Range(0, HttpClientProvider.MaxCachedClients + 20))
            HttpClientProvider.Get(new() { Credentials = new NetworkCredential($"bounded-{i}", "pw") });

        HttpClientProvider.CachedClientsCount.ShouldBeLessThanOrEqualTo(HttpClientProvider.MaxCachedClients);
    }

    [Fact]
    public async Task SendHttpAsync_ParamsWithCredentials_AuthenticateOnTheRealHandler()
    {
        using var server = new LoopbackServer(context =>
            context.Response.StatusCode = context.User?.Identity?.Name == "provider-user"
                ? 200
                : 403,
            AuthenticationSchemes.Basic);

        var pars = new WebRequestParams { Credentials = new NetworkCredential("provider-user", "pw") };

        using var response = await server.Url.SendHttpAsync(pars, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendHttpAsync_SetCookieOnTheSharedClient_IsNotReplayedToOtherCallers()
    {
        var seenCookies = new System.Collections.Concurrent.ConcurrentQueue<string?>();
        using var server = new LoopbackServer(context =>
        {
            seenCookies.Enqueue(context.Request.Headers["Cookie"]);
            context.Response.AddHeader("Set-Cookie", "session=userA; Path=/");
        });

        var token = TestContext.Current.CancellationToken;

        using (await server.Url.SendHttpAsync(cancellationToken: token))
        {
        }

        var other = new WebRequestParams { Headers = new System.Collections.Generic.Dictionary<string, string> { ["Cookie"] = "session=userB" } };

        using (await server.Url.SendHttpAsync(other, cancellationToken: token))
        {
        }

        seenCookies.ToArray().ShouldBe([null, "session=userB"]);
    }

    [Fact]
    public async Task SendHttpAsync_CookieContainerInParams_KeepsCookiesBetweenCalls()
    {
        var seenCookies = new System.Collections.Concurrent.ConcurrentQueue<string?>();
        using var server = new LoopbackServer(context =>
        {
            seenCookies.Enqueue(context.Request.Headers["Cookie"]);
            context.Response.AddHeader("Set-Cookie", "session=kept; Path=/");
        });

        var token = TestContext.Current.CancellationToken;
        var pars = new WebRequestParams { Cookies = new() };

        using (await server.Url.SendHttpAsync(pars, cancellationToken: token))
        {
        }

        using (await server.Url.SendHttpAsync(pars, cancellationToken: token))
        {
        }

        seenCookies.ToArray().ShouldBe([null, "session=kept"]);
    }

    [Fact]
    public async Task SendHttpAsync_ServerNeverAnswers_TimesOutWithTheDefaultTimeout()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var url = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/");
        var accepted = listener.AcceptTcpClientAsync(TestContext.Current.CancellationToken);

        // No params and no token: only the default timeout can end this request (200 ms stands in for the 100 s).
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            using var _ = await url.SendHttpAsync(null, null, null, true, 200, null, CancellationToken.None);
        });

        using var connection = await accepted;
    }

    [Fact]
    public void DefaultTimeout_MatchesHttpWebRequest() => UriExtensions.DefaultTimeoutMilliseconds.ShouldBe(100_000);

    [Fact]
    public void GetHttpClientHandler_IsProxyNull_DisablesTheProxy()
    {
        using var handler = new WebRequestParams { IsProxyNull = true }.GetHttpClientHandler();

        handler.UseProxy.ShouldBeFalse();
        handler.Proxy.ShouldBeNull();
    }

    [Fact]
    public void GetHttpClientHandler_DoesNotOfferClientCertificates()
    {
        using var handler = new WebRequestParams { Credentials = new NetworkCredential("u", "p") }.GetHttpClientHandler();

        handler.ClientCertificateOptions.ShouldBe(ClientCertificateOption.Manual);
    }

    [Fact]
    public void GetHttpClientHandler_CertificateCallback_IsInvoked()
    {
        var calls = 0;
        using var handler = new WebRequestParams
        {
            ServerCertificateValidationCallback = (_, _, _, errors) =>
            {
                calls++;

                return errors == SslPolicyErrors.RemoteCertificateNameMismatch;
            }
        }.GetHttpClientHandler();

        using var message = new HttpRequestMessage();

        handler.ServerCertificateCustomValidationCallback!(message, null, null, SslPolicyErrors.RemoteCertificateNameMismatch)
            .ShouldBeTrue();

        calls.ShouldBe(1);
    }
}
