using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Abstractions.Extensions;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace FEx.Downloader.Clients;

// SYSLIB0014: This type is an extended WebClient by design; migrating the downloader to
// HttpClient is out of scope. Behavior retained for legacy download/cookie-persistence support.
#pragma warning disable SYSLIB0014

/// <summary>
/// An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent
/// requests.
/// </summary>
public sealed class FlakWebClient : WebClient
{
    public static List<string> Unreachable { get; } = [];

    public WebRequestParams Pars { get; }
    public NotifyProgressInfoChanged ProgressInfo { get; }
    public HttpStatusCode StatusCode { get; private set; }
    public bool HeadOnly { get; set; }

    public Uri ResponseUri { get; private set; }

    public Uri RequestUri { get; private set; }

    public Uri DownloadedFileAddress { get; private set; }

    public FlakWebClient()
        : this(null, null)
    {
    }

    public FlakWebClient(WebRequestParams pars)
        : this(pars, null)
    {
    }

    public FlakWebClient(WebRequestParams pars,
                         Action<object, DownloadProgressChangedEventArgs> downloadProgressHandler)
    {
        pars ??= new();

        Pars = pars;

        Pars.Cookies ??= new();

        Pars.Timeout ??= 100000;

        ProgressInfo = new();
        this.PrepareWebClient(Pars);

        if (downloadProgressHandler is not null)
            DownloadProgressChanged += (x, y) => downloadProgressHandler(x, y);
        //todo attach via observable
    }

    /// <summary>
    /// Returns list of cookies.
    /// </summary>
    /// <returns></returns>
    public List<Cookie> CookieMonster()
    {
        var table = (Hashtable)Pars.Cookies.GetType()
            .InvokeMember("m_domainTable",
                BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance,
                null,
                Pars.Cookies,
                []);

        return [.. table.Keys.Cast<object>()
            .SelectMany(key => Pars.Cookies.GetCookies(new($"http://{key}/"))
#if NETSTANDARD
                    .Cast<Cookie>()
#endif
                ,
                (_, cookie) => cookie)];
    }

    public async Task DownloadFileWithProgressAsync(Uri address, string filePath)
    {
        DownloadedFileAddress = address;
        //if (ProgressViewModel is not null)
        //{
        //    ProgressViewModel.ProgressIsIndeterminate = true;
        //}

        //TotalBytesToReceive = 0;
        //BytesReceived = 0;

        if (filePath is not null)
        {
            var folder = Path.GetDirectoryName(filePath);

            if (folder is not null)
                Directory.CreateDirectory(folder);

            //WeakEventManager<FlakWebClient, DownloadProgressChangedEventArgs>.AddHandler(this, nameof(DownloadProgressChanged), CallbackOfDownloadProgress);
            await DownloadFileTaskAsync(DownloadedFileAddress, filePath);
        }
    }

    public async Task DownloadFileWithProgressAsync(string address, string filePath) =>
        await DownloadFileWithProgressAsync(new Uri(address), filePath);

    protected override WebResponse GetWebResponse(WebRequest request)
    {
        try
        {
            var res = base.GetWebResponse(request);
            ReadCookies(res);
            var response = (HttpWebResponse)res;

            if (response is not null)
            {
                ResponseUri = response.StatusCode != HttpStatusCode.OK
                    ? null
                    : response.ResponseUri;

                StatusCode = response.StatusCode;
            }

            return response;
        }
        catch (Exception ex)
        {
            ex.HandleException(false);
            var exception = ex as WebException;

            if ((HttpWebResponse)exception?.Response is not null
                && ((HttpWebResponse)exception.Response).StatusCode == HttpStatusCode.NotFound)
                Unreachable.Add(request.RequestUri.AbsoluteUri);
        }

        return null;
    }

    protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
    {
        //try
        //{
        var res = base.GetWebResponse(request, result);
        ReadCookies(res);

        if (request is not HttpWebRequest)
            return res;

        var response = (HttpWebResponse)res;
        ResponseUri = response.ResponseUri;

        //if (response.StatusCode != HttpStatusCode.OK)
        //{
        //    ResponseUri = null;
        //}
        return response;

        //}
        //catch (Exception ex)
        //{
        //    ex.HandleException(false);
        //    Unreachable.Add(request.RequestUri.AbsoluteUri);
        //}
        //return null;
    }

    protected override WebRequest GetWebRequest(Uri address)
    {
        var request = base.GetWebRequest(address);
        var req = request as HttpWebRequest;

        if (req is not null)
        {
            req.PrepareRequest(Pars);

            if (HeadOnly && req.Method == "GET")
                req.Method = "HEAD";

            if (Pars.Timeout is not null)
                req.Timeout = Pars.Timeout.Value;

            RequestUri = req.RequestUri;
            //req.AllowAutoRedirect = false;
            req.ServicePoint.ConnectionLimit = int.MaxValue;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            return req;
        }

        return request;
    }

    protected override void OnDownloadProgressChanged(DownloadProgressChangedEventArgs e)
    {
        base.OnDownloadProgressChanged(e);
        ProgressInfo.Value = e.BytesReceived;
        ProgressInfo.Maximum = e.TotalBytesToReceive;
        ProgressInfo.ChangeMode = ProgressChangeMode.Set;
    }

    private void ReadCookies(WebResponse r)
    {
        if (r is not HttpWebResponse response)
            return;

        var cookies = response.Cookies;
        Pars.Cookies.Add(cookies);
    }
}