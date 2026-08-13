using FEx.WebScraping.Abstractions.Interfaces;
using HtmlAgilityPack;
using System;
using System.Threading.Tasks;

namespace FEx.WebScraping.Extensions;

public static class WebBrowserScraperExtensions
{
    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebBrowserScraper scraper, Uri pageLink) =>
        scraper.LoadHtmlDocumentAsync(pageLink, null, null);

    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebBrowserScraper scraper,
                                                           Uri pageLink,
                                                           Action<HtmlWeb> configWeb) =>
        scraper.LoadHtmlDocumentAsync(pageLink, configWeb, null);

    public static Task LoadAsync(this IWebBrowserScraper scraper,
                                 Uri pageLink,
                                 Action<Uri, HtmlWeb, HtmlDocument> action) =>
        scraper.LoadAsync(pageLink, action, null, null);

    public static Task LoadAsync(this IWebBrowserScraper scraper,
                                 Uri pageLink,
                                 Action<Uri, HtmlWeb, HtmlDocument> action,
                                 Action<HtmlWeb> configWeb) =>
        scraper.LoadAsync(pageLink, action, configWeb, null);

    public static Task<T> LoadAsync<T>(this IWebBrowserScraper scraper,
                                       Uri pageLink,
                                       Func<Uri, HtmlWeb, HtmlDocument, T> action) =>
        scraper.LoadAsync(pageLink, action, null, null);

    public static Task<T> LoadAsync<T>(this IWebBrowserScraper scraper,
                                       Uri pageLink,
                                       Func<Uri, HtmlWeb, HtmlDocument, T> action,
                                       Action<HtmlWeb> configWeb) =>
        scraper.LoadAsync(pageLink, action, configWeb, null);

    public static T
        Load<T>(this IWebBrowserScraper scraper, Uri pageLink, Func<Uri, HtmlWeb, HtmlDocument, T> action) =>
        scraper.Load(pageLink, action, null, null);

    public static T Load<T>(this IWebBrowserScraper scraper,
                            Uri pageLink,
                            Func<Uri, HtmlWeb, HtmlDocument, T> action,
                            Action<HtmlWeb> configWeb) =>
        scraper.Load(pageLink, action, configWeb, null);

    public static (HtmlWeb web, HtmlDocument doc) Load(this IWebBrowserScraper scraper, Uri pageLink) =>
        scraper.Load(pageLink, null, null);

    public static (HtmlWeb web, HtmlDocument doc) Load(this IWebBrowserScraper scraper,
                                                       Uri pageLink,
                                                       Action<HtmlWeb> configWeb) =>
        scraper.Load(pageLink, configWeb, null);
}