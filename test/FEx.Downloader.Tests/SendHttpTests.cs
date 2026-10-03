using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Abstractions.Extensions;
using Shouldly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Downloader.Tests;

public sealed class SendHttpTests
{
    private static readonly Uri Url = new("http://stub/file.bin");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SendHttpAsync_AppliesMethodHeadersUserAgentAndRange()
    {
        var handler = new FakeHttpHandler(_ => new(HttpStatusCode.PartialContent));
        using var client = new HttpClient(handler);
        var pars = new WebRequestParams
        {
            Method = "HEAD",
            UserAgent = "fex-tests",
            Headers = new System.Collections.Generic.Dictionary<string, string> { ["X-Custom"] = "1" }
        };

        using var _ = await Url.SendHttpAsync(pars, client, new(10, 19), cancellationToken: Ct);

        var request = handler.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Head);
        request.Headers.Range!.ToString().ShouldBe("bytes=10-19");
        request.Headers.UserAgent.ToString().ShouldBe("fex-tests");
        request.Headers.GetValues("X-Custom").ShouldBe(["1"]);
    }

    [Fact]
    public async Task SendHttpAsync_NonSuccessStatus_ThrowsWithTheStatus()
    {
        using var client = new HttpClient(new FakeHttpHandler(_ => new(HttpStatusCode.Forbidden)));

        var ex = await Should.ThrowAsync<HttpStatusException>(async () =>
        {
            using var _ = await Url.SendHttpAsync(client: client, cancellationToken: Ct);
        });

        ex.ResponseStatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SendHttpAsync_NonSuccessStatusWithoutEnsure_ReturnsTheResponse()
    {
        using var client = new HttpClient(new FakeHttpHandler(_ => new(HttpStatusCode.NotFound)));

        using var response = await Url.SendHttpAsync(client: client, ensureSuccess: false, cancellationToken: Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 10_000)]
    public async Task SendHttpAsync_TimeoutFromParams_CancelsTheRequest()
    {
        using var client = new HttpClient(new FakeHttpHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);

            return new(HttpStatusCode.OK);
        }));

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            using var _ = await Url.SendHttpAsync(new() { Timeout = 50 },
                client,
                cancellationToken: TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task SendHttpAsync_CallerCancellation_CancelsTheRequest()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var client = new HttpClient(new FakeHttpHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);

            return new(HttpStatusCode.OK);
        }));

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            using var _ = await Url.SendHttpAsync(client: client, cancellationToken: cts.Token);
        });
    }

    [Fact]
    public async Task TryGetRangeAsync_ServerAcceptingRanges_ReturnsTrue()
    {
        var handler = new FakeHttpHandler(request =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.PartialContent)
            {
                Content = new ByteArrayContent([1])
            };

            response.Content.Headers.ContentRange = new(0, 0, 10);

            return response;
        });

        using var client = new HttpClient(handler);
        using var probe = new HttpResponseMessage(HttpStatusCode.OK);
        probe.Headers.AcceptRanges.Add("bytes");

        var (supported, _) = await Url.TryGetRangeAsync(probe, 0, 0, null, client, Ct);

        supported.ShouldBeTrue();
        handler.Requests.Single().Headers.Range!.ToString().ShouldBe("bytes=0-0");
    }

    [Fact]
    public async Task TryGetRangeAsync_ServerNotAdvertisingRanges_ReturnsFalseWithoutRequest()
    {
        var handler = new FakeHttpHandler(_ => new(HttpStatusCode.PartialContent));
        using var client = new HttpClient(handler);
        using var probe = new HttpResponseMessage(HttpStatusCode.OK);

        var (supported, _) = await Url.TryGetRangeAsync(probe, 0, 0, null, client, Ct);

        supported.ShouldBeFalse();
        handler.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NoContent, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task CheckForInternetConnectionAsync_ReflectsTheResponseStatus(HttpStatusCode status, bool expected)
    {
        using var client = new HttpClient(new FakeHttpHandler(_ => new(status)));

        (await Url.CheckForInternetConnectionAsync(client, Ct)).ShouldBe(expected);
    }

    [Fact]
    public async Task GetHttpFileSizeAsync_ReadsTheContentLengthWithoutTheBody()
    {
        using var client = new HttpClient(new FakeHttpHandler(_ => new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[7])
        }));

        (await Url.GetHttpFileSizeAsync(client: client, cancellationToken: Ct)).ShouldBe(7);
    }

    [Fact]
    public async Task UrlIsValidAsync_NotFound_ReportsAnError()
    {
        using var client = new HttpClient(new FakeHttpHandler(_ => new(HttpStatusCode.NotFound)));

        var result = await Url.UrlIsValidAsync(null, client);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task SendHttpAsync_MethodOverride_DoesNotChangeTheCallersParams()
    {
        var handler = new FakeHttpHandler(_ => new(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        var pars = new WebRequestParams { Method = "GET" };

        using var _ = await Url.SendHttpAsync(pars, client, method: HttpMethod.Head, cancellationToken: Ct);

        handler.Requests.Single().Method.ShouldBe(HttpMethod.Head);
        pars.Method.ShouldBe("GET");
    }

    [Fact]
    public async Task UrlIsValidAsync_DoesNotWriteHeadIntoTheCallersParams()
    {
        var handler = new FakeHttpHandler(_ => new(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        var pars = new WebRequestParams();

        (await Url.UrlIsValidAsync(pars, client)).IsSuccess.ShouldBeTrue();

        handler.Requests.Single().Method.ShouldBe(HttpMethod.Head);
        pars.Method.ShouldBeNull();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task SendHttpAsync_KeepAlive_MapsToTheConnectionHeader(bool keepAlive, bool expectedClose)
    {
        var handler = new FakeHttpHandler(_ => new(HttpStatusCode.OK));
        using var client = new HttpClient(handler);

        using var _ = await Url.SendHttpAsync(new() { KeepAlive = keepAlive }, client, cancellationToken: Ct);

        handler.Requests.Single().Headers.ConnectionClose.ShouldBe(expectedClose);
    }

    [Fact]
    public async Task SendHttpAsync_RetryAfterHeader_IsExposedOnTheException()
    {
        using var client = new HttpClient(new FakeHttpHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new(TimeSpan.FromSeconds(7));

            return response;
        }));

        var ex = await Should.ThrowAsync<HttpStatusException>(async () =>
        {
            using var _ = await Url.SendHttpAsync(client: client, cancellationToken: Ct);
        });

        ex.RetryAfter.ShouldBe(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task SendHttpAsync_FailingUrlWithSecrets_DoesNotLeakThemIntoTheMessage()
    {
        using var client = new HttpClient(new FakeHttpHandler(_ => new(HttpStatusCode.Forbidden)));
        var secretUrl = new Uri("https://user:pa55word@host.example/path/f.bin?token=SECRET");

        var ex = await Should.ThrowAsync<HttpStatusException>(async () =>
        {
            using var _ = await secretUrl.SendHttpAsync(client: client, cancellationToken: Ct);
        });

        ex.Message.ShouldContain("https://host.example/path/f.bin");
        ex.Message.ShouldNotContain("SECRET");
        ex.Message.ShouldNotContain("pa55word");
    }

    [Fact]
    public void GetAllHeaders_IncludesTheContentHeaders()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };

        response.Content.Headers.ContentLength = 3;
        response.Content.Headers.ContentDisposition = new("attachment") { FileName = "a.bin" };
        response.Headers.Add("X-Server", "s");

        var headers = response.GetAllHeaders();

        headers.ShouldContainKey("Content-Disposition");
        headers.ShouldContainKey("Content-Length");
        headers.ShouldContainKey("X-Server");
    }

    [Fact]
    public async Task CheckForInternetConnectionAsync_CancelledToken_Cancels()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        using var client = new HttpClient(new FakeHttpHandler((_, token) =>
        {
            token.ThrowIfCancellationRequested();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }));

        // a cancelled caller must not be told "offline"
        await Should.ThrowAsync<OperationCanceledException>(() => Url.CheckForInternetConnectionAsync(client, cts.Token));
    }

    [Fact]
    public void GetWebRequest_HttpUrl_IsRejected() =>
        Should.Throw<ArgumentException>(() => Url.GetWebRequest());
}
