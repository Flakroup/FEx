using FEx.WebScraping.Abstractions.Interfaces;
using HtmlAgilityPack;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.WebScraping.Extensions;

public static class WebBrowserScraperExtensions
{
    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebBrowserScraper scraper, Uri pageLink,
        CancellationToken cancellationToken = default) =>
        scraper.LoadHtmlDocumentAsync(pageLink, null, null, cancellationToken);

    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebBrowserScraper scraper,
                                                           Uri pageLink,
                                                           Action<HtmlWeb> configWeb,
                                                           CancellationToken cancellationToken = default) =>
        scraper.LoadHtmlDocumentAsync(pageLink, configWeb, null, cancellationToken);

    public static Task LoadAsync(this IWebBrowserScraper scraper,
                                 Uri pageLink,
                                 Action<Uri, HtmlWeb, HtmlDocument> action,
                                 CancellationToken cancellationToken = default) =>
        scraper.LoadAsync(pageLink, action, null, null, cancellationToken);

    public static Task LoadAsync(this IWebBrowserScraper scraper,
                                 Uri pageLink,
                                 Action<Uri, HtmlWeb, HtmlDocument> action,
                                 Action<HtmlWeb> configWeb,
                                 CancellationToken cancellationToken = default) =>
        scraper.LoadAsync(pageLink, action, configWeb, null, cancellationToken);

    public static Task<T> LoadAsync<T>(this IWebBrowserScraper scraper,
                                       Uri pageLink,
                                       Func<Uri, HtmlWeb, HtmlDocument, T> action,
                                       CancellationToken cancellationToken = default) =>
        scraper.LoadAsync(pageLink, action, null, null, cancellationToken);

    public static Task<T> LoadAsync<T>(this IWebBrowserScraper scraper,
                                       Uri pageLink,
                                       Func<Uri, HtmlWeb, HtmlDocument, T> action,
                                       Action<HtmlWeb> configWeb,
                                       CancellationToken cancellationToken = default) =>
        scraper.LoadAsync(pageLink, action, configWeb, null, cancellationToken);

    public static T
        Load<T>(this IWebBrowserScraper scraper, Uri pageLink, Func<Uri, HtmlWeb, HtmlDocument, T> action,
        CancellationToken cancellationToken = default) =>
        scraper.Load(pageLink, action, null, null, cancellationToken);

    public static T Load<T>(this IWebBrowserScraper scraper,
                            Uri pageLink,
                            Func<Uri, HtmlWeb, HtmlDocument, T> action,
                            Action<HtmlWeb> configWeb,
                            CancellationToken cancellationToken = default) =>
        scraper.Load(pageLink, action, configWeb, null, cancellationToken);

    public static (HtmlWeb web, HtmlDocument doc) Load(this IWebBrowserScraper scraper, Uri pageLink,
        CancellationToken cancellationToken = default) =>
        scraper.Load(pageLink, null, null, cancellationToken);

    public static (HtmlWeb web, HtmlDocument doc) Load(this IWebBrowserScraper scraper,
                                                       Uri pageLink,
                                                       Action<HtmlWeb> configWeb,
                                                       CancellationToken cancellationToken = default) =>
        scraper.Load(pageLink, configWeb, null, cancellationToken);
}