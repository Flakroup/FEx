using HtmlAgilityPack;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.WebScraping.Abstractions.Interfaces;

/// <summary>
/// Browser-driven scraping. Every member takes an optional <see cref="System.Threading.CancellationToken" />;
/// implementers written against the earlier token-less signatures must add the parameter (source-breaking).
/// </summary>
public interface IWebBrowserScraper
{
    Task<HtmlDocument> LoadHtmlDocumentAsync(Uri pageLink,
                                             Action<HtmlWeb>? configWeb,
                                             Func<string, bool>? isBrowserScriptCompleted,
                                             CancellationToken cancellationToken = default);

    Task LoadAsync(Uri pageLink,
                   Action<Uri, HtmlWeb, HtmlDocument> action,
                   Action<HtmlWeb>? configWeb,
                   Func<string, bool>? isBrowserScriptCompleted,
                   CancellationToken cancellationToken = default);

    Task<T> LoadAsync<T>(Uri pageLink,
                         Func<Uri, HtmlWeb, HtmlDocument, T> action,
                         Action<HtmlWeb>? configWeb,
                         Func<string, bool>? isBrowserScriptCompleted,
                         CancellationToken cancellationToken = default);

    T Load<T>(Uri pageLink,
              Func<Uri, HtmlWeb, HtmlDocument, T> action,
              Action<HtmlWeb>? configWeb,
              Func<string, bool>? isBrowserScriptCompleted,
              CancellationToken cancellationToken = default);

    (HtmlWeb web, HtmlDocument doc) Load(Uri pageLink,
                                         Action<HtmlWeb>? configWeb,
                                         Func<string, bool>? isBrowserScriptCompleted,
                                         CancellationToken cancellationToken = default);
}