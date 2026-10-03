using FEx.OneDrv.Abstractions;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Tests;

// IDISP004/IDISP005: the fake client and its handler live for the duration of one test.
#pragma warning disable IDISP001, IDISP004, IDISP005

/// <summary>Fakes Microsoft Graph at the HTTP layer so tests exercise the real request builders and paging.</summary>
internal sealed class FakeGraph : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public List<string> Requests { get; } = [];

    public FakeGraph(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    public IGraphServiceClientCache CreateCache()
    {
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: new HttpClient(this));
        var client = new GraphServiceClient(adapter);
        var cache = Substitute.For<IGraphServiceClientCache>();
        cache.GetAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult((client, "drive-1")));

        return cache;
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    public static string File(string id) =>
        "{\"id\":\"" + id + "\",\"name\":\"" + id + ".txt\",\"file\":{\"mimeType\":\"text/plain\"}}";

    public static string Folder(string id) =>
        "{\"id\":\"" + id + "\",\"name\":\"" + id + "\",\"folder\":{\"childCount\":0}}";

    public static string Page(string[] items, string? nextLink = null) =>
        "{\"value\":[" + string.Join(",", items) + "]"
        + (nextLink is null ? "" : ",\"@odata.nextLink\":\"" + nextLink + "\"")
        + "}";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                           CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!.ToString());

        return Task.FromResult(_respond(request));
    }
}
