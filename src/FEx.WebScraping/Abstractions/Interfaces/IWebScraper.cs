using HtmlAgilityPack;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.WebScraping.Abstractions.Interfaces;

public interface IWebScraper
{
    Task<HtmlDocument> LoadHtmlDocumentAsync(Uri pageLink,
                                             Action<HtmlWeb>? configWeb,
                                             Encoding? encoding,
                                             NetworkCredential? credential,
                                             CancellationToken cancellationToken);

    Task<T> LoadAsync<T>(Uri pageLink,
                         Func<Uri, HtmlWeb, HtmlDocument, T> action,
                         Action<HtmlWeb>? configWeb,
                         Encoding? encoding,
                         NetworkCredential? credential,
                         CancellationToken cancellationToken);

    Task<HtmlDocument> LoadHtmlDocumentAsync(HtmlWeb web,
                                             Uri pageLink,
                                             Encoding? encoding,
                                             NetworkCredential? credential,
                                             CancellationToken cancellationToken);

    (HtmlWeb web, Task<HtmlDocument> docTask) Load(Uri pageLink,
                                                   Action<HtmlWeb>? configWeb,
                                                   Encoding? encoding,
                                                   NetworkCredential? credential,
                                                   CancellationToken cancellationToken);
}
