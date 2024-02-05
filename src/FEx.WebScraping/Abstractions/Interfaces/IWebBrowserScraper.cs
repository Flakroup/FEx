using HtmlAgilityPack;
using System;
using System.Threading.Tasks;

namespace FEx.WebScraping.Abstractions.Interfaces;

public interface IWebBrowserScraper
{
    Task<HtmlDocument> LoadHtmlDocumentAsync(Uri pageLink,
                                                    Action<HtmlWeb> configWeb = null,
                                                    Func<string, bool> isBrowserScriptCompleted = null);

    Task LoadAsync(Uri pageLink,
                                  Action<Uri, HtmlWeb, HtmlDocument> action,
                                  Action<HtmlWeb> configWeb = null,
                                  Func<string, bool> isBrowserScriptCompleted = null);

    Task<T> LoadAsync<T>(Uri pageLink,
                                        Func<Uri, HtmlWeb, HtmlDocument, T> action,
                                        Action<HtmlWeb> configWeb = null,
                                        Func<string, bool> isBrowserScriptCompleted = null);

    T Load<T>(Uri pageLink,
                         Func<Uri, HtmlWeb, HtmlDocument, T> action,
                         Action<HtmlWeb> configWeb = null,
                         Func<string, bool> isBrowserScriptCompleted = null);

    (HtmlWeb web, HtmlDocument doc) Load(Uri pageLink,
                                                    Action<HtmlWeb> configWeb = null,
                                                    Func<string, bool> isBrowserScriptCompleted = null);
}