using FEx.WebScraping.Abstractions.Interfaces;
using HtmlAgilityPack;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.WebScraping.Extensions;

public static class WebScraperExtensions
{
    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebScraper scraper, Uri pageLink) =>
        scraper.LoadHtmlDocumentAsync(pageLink, null, null, null, default);

    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebScraper scraper, Uri pageLink,
                                                           Action<HtmlWeb> configWeb) =>
        scraper.LoadHtmlDocumentAsync(pageLink, configWeb, null, null, default);

    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebScraper scraper, Uri pageLink,
                                                           Action<HtmlWeb> configWeb,
                                                           Encoding encoding) =>
        scraper.LoadHtmlDocumentAsync(pageLink, configWeb, encoding, null, default);

    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebScraper scraper, Uri pageLink,
                                                           Action<HtmlWeb> configWeb,
                                                           Encoding encoding,
                                                           NetworkCredential credential) =>
        scraper.LoadHtmlDocumentAsync(pageLink, configWeb, encoding, credential, default);

    public static Task<T> LoadAsync<T>(this IWebScraper scraper, Uri pageLink,
                                       Func<Uri, HtmlWeb, HtmlDocument, T> action) =>
        scraper.LoadAsync(pageLink, action, null, null, null, default);

    public static Task<T> LoadAsync<T>(this IWebScraper scraper, Uri pageLink,
                                       Func<Uri, HtmlWeb, HtmlDocument, T> action,
                                       Action<HtmlWeb> configWeb) =>
        scraper.LoadAsync(pageLink, action, configWeb, null, null, default);

    public static Task<T> LoadAsync<T>(this IWebScraper scraper, Uri pageLink,
                                       Func<Uri, HtmlWeb, HtmlDocument, T> action,
                                       Action<HtmlWeb> configWeb,
                                       Encoding encoding) =>
        scraper.LoadAsync(pageLink, action, configWeb, encoding, null, default);

    public static Task<T> LoadAsync<T>(this IWebScraper scraper, Uri pageLink,
                                       Func<Uri, HtmlWeb, HtmlDocument, T> action,
                                       Action<HtmlWeb> configWeb,
                                       Encoding encoding,
                                       NetworkCredential credential) =>
        scraper.LoadAsync(pageLink, action, configWeb, encoding, credential, default);

    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebScraper scraper, HtmlWeb web, Uri pageLink) =>
        scraper.LoadHtmlDocumentAsync(web, pageLink, null, null, default);

    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebScraper scraper, HtmlWeb web, Uri pageLink,
                                                           Encoding encoding) =>
        scraper.LoadHtmlDocumentAsync(web, pageLink, encoding, null, default);

    public static Task<HtmlDocument> LoadHtmlDocumentAsync(this IWebScraper scraper, HtmlWeb web, Uri pageLink,
                                                           Encoding encoding,
                                                           NetworkCredential credential) =>
        scraper.LoadHtmlDocumentAsync(web, pageLink, encoding, credential, default);

    public static (HtmlWeb web, Task<HtmlDocument> docTask) Load(this IWebScraper scraper, Uri pageLink) =>
        scraper.Load(pageLink, null, null, null, default);

    public static (HtmlWeb web, Task<HtmlDocument> docTask) Load(this IWebScraper scraper, Uri pageLink,
                                                                 Action<HtmlWeb> configWeb) =>
        scraper.Load(pageLink, configWeb, null, null, default);

    public static (HtmlWeb web, Task<HtmlDocument> docTask) Load(this IWebScraper scraper, Uri pageLink,
                                                                 Action<HtmlWeb> configWeb,
                                                                 Encoding encoding) =>
        scraper.Load(pageLink, configWeb, encoding, null, default);

    public static (HtmlWeb web, Task<HtmlDocument> docTask) Load(this IWebScraper scraper, Uri pageLink,
                                                                 Action<HtmlWeb> configWeb,
                                                                 Encoding encoding,
                                                                 NetworkCredential credential) =>
        scraper.Load(pageLink, configWeb, encoding, credential, default);
}
