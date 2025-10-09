using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Abstractions.Extensions;
using FEx.Webx.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

namespace FEx.Webx.Extensions;

public static class UriExtensions
{
    public static async Task<string> GetFileNameAsync(this Uri url, WebRequestParams pars = null) =>
        await url.DoHttpResponseFuncAsync((response, _) => response.GetFileName(), pars);

    public static string GetFileName(this HttpWebResponse response)
    {
        Dictionary<string, string> responseHeaders = response.GetAllHeaders();

        return GetFileName(response.ResponseUri, responseHeaders);
    }

    public static string GetFileName(this HttpResponseMessage response)
    {
        Dictionary<string, string[]> responseHeaders = response.GetAllHeaders();

        return GetFileName(response.RequestMessage!.RequestUri, responseHeaders);
    }

    public static async Task<string> DownloadStringAsync(this Uri url)
    {
        using var a = new HttpClient();

        return await a.GetStringAsync(url);
    }

    private static string GetFileName(Uri responseUri, IDictionary<string, string> responseHeaders)
    {
        string contentDispositionHeader = responseHeaders.Keys.FindInEnumerable(x => x.IsEqual("content-disposition"));

        return GetFileName(responseUri,
            contentDispositionHeader is not null
                ? responseHeaders[contentDispositionHeader]
                : null,
            responseHeaders.ToDictionary(x => x.Key, x => new[] { x.Value }));
    }

    private static string GetFileName(Uri responseUri, IDictionary<string, string[]> responseHeaders)
    {
        string contentDispositionHeader = responseHeaders.Keys.FindInEnumerable(x => x.IsEqual("content-disposition"));

        return GetFileName(responseUri,
            contentDispositionHeader is not null
                ? responseHeaders[contentDispositionHeader][0]
                : null,
            responseHeaders);
    }

    private static string GetFileName(Uri responseUri,
                                      string contentDispositionHeaderValue,
                                      IDictionary<string, string[]> responseHeaders)
    {
        if (contentDispositionHeaderValue is not null)
        {
            var values = contentDispositionHeaderValue.Split(';')
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0].Trim(),
                    x => x.Length > 1
                        ? x[1]
                        : null);

            string fileNameKey = values.Keys.FirstOrDefault(x => x.IsEqual("filename"));

            if (fileNameKey is not null
                && values.TryGetKeyValue(fileNameKey).IsNotNullOrEmptyString())
            {
                var contentDisposition = new ContentDisposition(contentDispositionHeaderValue);

                if (contentDisposition.FileName is not null)
                    return Uri.UnescapeDataString(contentDisposition.FileName);
            }
        }

        string fName = Uri.UnescapeDataString(responseUri.Segments.Last());
        string ext = Path.GetExtension(fName);

        string mimeType = responseHeaders.Where(x => x.Key.IsEqual("Content-Type"))
            .Select(x => x.Value)
            .SingleOrDefault()
            ?.FirstOrDefault()
            ?.Split(';')[0];

        IReadOnlyCollection<string> exts = null;

        if (mimeType.IsNotNullOrEmptyString())
            exts = MimeTypesUtility.GetDefaultExtensions(mimeType);

        if (exts.IsNotNullOrEmptyReadOnlyCollection()
            && (ext.TrimStart('.').IsNullOrEmptyString() || !exts.Contains(ext.TrimStart('.')) && !exts.Contains(".*")))
            fName = $"{fName}.{exts.FirstOrDefault()}";

        return fName;
    }
}