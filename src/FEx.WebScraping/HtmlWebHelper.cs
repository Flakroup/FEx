using FEx.Extensions.Helpers;
using FEx.WebScraping.Abstractions.Interfaces;
using HtmlAgilityPack;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.WebScraping;

public class HtmlWebHelper : IWebScraper
{
    public async Task<HtmlDocument> LoadHtmlDocumentAsync(Uri pageLink,
                                                          Action<HtmlWeb> configWeb = null,
                                                          Encoding encoding = null,
                                                          NetworkCredential credential = null,
                                                          CancellationToken cancellationToken = default) =>
        await LoadAsync(pageLink, (_, _, d) => d, configWeb, encoding, credential, cancellationToken);

    public async Task<T> LoadAsync<T>(Uri pageLink,
                                      Func<Uri, HtmlWeb, HtmlDocument, T> action,
                                      Action<HtmlWeb> configWeb = null,
                                      Encoding encoding = null,
                                      NetworkCredential credential = null,
                                      CancellationToken cancellationToken = default)
    {
        (HtmlWeb web, Task<HtmlDocument> docTask) = Load(pageLink, configWeb, encoding, credential, cancellationToken);
        HtmlDocument doc = await docTask;

        return action(web.ResponseUri, web, doc);
    }

    public (HtmlWeb web, Task<HtmlDocument> docTask) Load(Uri pageLink,
                                                          Action<HtmlWeb> configWeb = null,
                                                          Encoding encoding = null,
                                                          NetworkCredential credential = null,
                                                          CancellationToken cancellationToken = default)
    {
        var web = new HtmlWeb();
        configWeb?.Invoke(web);

        Task<HtmlDocument> task = StaticAsyncHelper
            .ExecuteTaskOnThreadPoolAsync(() => LoadHtmlDocumentAsync(web, pageLink, encoding, credential, cancellationToken));

        return (web, task);
    }

    public async Task<HtmlDocument> LoadHtmlDocumentAsync(HtmlWeb web,
                                              Uri pageLink,
                                              Encoding encoding = null,
                                              NetworkCredential credential = null,
                                              CancellationToken cancellationToken = default) =>
        await web.LoadFromWebAsync(pageLink, encoding, credential, cancellationToken);
}