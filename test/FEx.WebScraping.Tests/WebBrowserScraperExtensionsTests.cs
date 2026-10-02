using FEx.WebScraping.Abstractions.Interfaces;
using FEx.WebScraping.Extensions;
using HtmlAgilityPack;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.WebScraping.Tests;

public sealed class WebBrowserScraperExtensionsTests
{
    private static readonly Uri Link = new("http://example.invalid/");

    [Fact]
    public async Task LoadHtmlDocumentAsync_Shorthand_ForwardsCancellationToken()
    {
        var scraper = Substitute.For<IWebBrowserScraper>();
        using var cts = new CancellationTokenSource();

        await scraper.LoadHtmlDocumentAsync(Link, cts.Token);

        await scraper.Received(1).LoadHtmlDocumentAsync(Link, null, null, cts.Token);
    }

    [Fact]
    public async Task LoadAsync_Shorthand_ForwardsCancellationToken()
    {
        var scraper = Substitute.For<IWebBrowserScraper>();
        using var cts = new CancellationTokenSource();
        Action<Uri, HtmlWeb, HtmlDocument> action = (_, _, _) => { };

        await scraper.LoadAsync(Link, action, cts.Token);

        await scraper.Received(1).LoadAsync(Link, action, null, null, cts.Token);
    }
}
