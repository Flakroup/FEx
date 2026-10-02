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
    private static readonly Action<Uri, HtmlWeb, HtmlDocument> Act = (_, _, _) => { };
    private static readonly Func<Uri, HtmlWeb, HtmlDocument, int> Fn = (_, _, _) => 1;
    private static readonly Action<HtmlWeb> Config = _ => { };

    private readonly IWebBrowserScraper _scraper = Substitute.For<IWebBrowserScraper>();
    private readonly CancellationTokenSource _cts = new();

    [Fact]
    public async Task LoadHtmlDocumentAsync_Link_ForwardsToken()
    {
        await _scraper.LoadHtmlDocumentAsync(Link, _cts.Token);
        await _scraper.Received(1).LoadHtmlDocumentAsync(Link, null, null, _cts.Token);
    }

    [Fact]
    public async Task LoadHtmlDocumentAsync_LinkConfig_ForwardsToken()
    {
        await _scraper.LoadHtmlDocumentAsync(Link, Config, _cts.Token);
        await _scraper.Received(1).LoadHtmlDocumentAsync(Link, Config, null, _cts.Token);
    }

    [Fact]
    public async Task LoadAsync_Action_ForwardsToken()
    {
        await _scraper.LoadAsync(Link, Act, _cts.Token);
        await _scraper.Received(1).LoadAsync(Link, Act, null, null, _cts.Token);
    }

    [Fact]
    public async Task LoadAsync_ActionConfig_ForwardsToken()
    {
        await _scraper.LoadAsync(Link, Act, Config, _cts.Token);
        await _scraper.Received(1).LoadAsync(Link, Act, Config, null, _cts.Token);
    }

    [Fact]
    public async Task LoadAsyncT_Func_ForwardsToken()
    {
        await _scraper.LoadAsync(Link, Fn, _cts.Token);
        await _scraper.Received(1).LoadAsync(Link, Fn, null, null, _cts.Token);
    }

    [Fact]
    public async Task LoadAsyncT_FuncConfig_ForwardsToken()
    {
        await _scraper.LoadAsync(Link, Fn, Config, _cts.Token);
        await _scraper.Received(1).LoadAsync(Link, Fn, Config, null, _cts.Token);
    }

    [Fact]
    public void LoadT_Func_ForwardsToken()
    {
        _scraper.Load(Link, Fn, _cts.Token);
        _scraper.Received(1).Load(Link, Fn, null, null, _cts.Token);
    }

    [Fact]
    public void LoadT_FuncConfig_ForwardsToken()
    {
        _scraper.Load(Link, Fn, Config, _cts.Token);
        _scraper.Received(1).Load(Link, Fn, Config, null, _cts.Token);
    }

    [Fact]
    public void Load_Link_ForwardsToken()
    {
        _scraper.Load(Link, _cts.Token);
        _scraper.Received(1).Load(Link, null, null, _cts.Token);
    }

    [Fact]
    public void Load_LinkConfig_ForwardsToken()
    {
        _scraper.Load(Link, Config, _cts.Token);
        _scraper.Received(1).Load(Link, Config, null, _cts.Token);
    }
}
