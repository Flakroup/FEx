using HtmlAgilityPack;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.WebScraping.Abstractions.Interfaces;

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