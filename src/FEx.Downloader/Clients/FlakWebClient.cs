using FEx.Extensions;
using FEx.Extensions.Base.Models;
using FEx.Extensions.Web;
using FEx.MVVM.Enums;
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

/// <summary>
///     An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent
///     requests.
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

    public FlakWebClient(WebRequestParams pars = null,
                         Action<object, DownloadProgressChangedEventArgs> downloadProgressHandler = null)
    {
        pars ??= new WebRequestParams();

        Pars = pars;

        Pars.Cookies ??= new CookieContainer();

        Pars.Timeout ??= 100000;

        ProgressInfo = new NotifyProgressInfoChanged();
        this.PrepareWebClient(Pars);

        if (downloadProgressHandler is not null)
            DownloadProgressChanged += (x, y) => downloadProgressHandler(x, y);
        //todo attach via observable
    }

    /// <summary>
    ///     Returns list of cookies.
    /// </summary>
    /// <returns></returns>
    public List<Cookie> CookieMonster()
    {
        var table = (Hashtable)Pars.Cookies.GetType()
            .InvokeMember("m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null,
                Pars.Cookies, new object[] { });

        return table.Keys.Cast<object>()
            .SelectMany(key => Pars.Cookies.GetCookies(new Uri($"http://{key}/"))
#if NETSTANDARD
                .Cast<Cookie>()
#endif
                , (_, cookie) => cookie)
            .ToList();
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
            string folder = Path.GetDirectoryName(filePath);

            if (folder is not null)
                Directory.CreateDirectory(folder);

            //WeakEventManager<FlakWebClient, DownloadProgressChangedEventArgs>.AddHandler(this, nameof(DownloadProgressChanged), CallbackOfDownloadProgress);
            await DownloadFileTaskAsync(DownloadedFileAddress, filePath);
        }
    }

    public async Task DownloadFileWithProgressAsync(string address, string filePath)
    {
        await DownloadFileWithProgressAsync(new Uri(address), filePath);
    }

    protected override WebResponse GetWebResponse(WebRequest request)
    {
        try
        {
            using WebResponse res = base.GetWebResponse(request);
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
        WebResponse res = base.GetWebResponse(request, result);
        ReadCookies(res);
        var req = request as HttpWebRequest;

        if (req is null)
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
        WebRequest request = base.GetWebRequest(address);
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
        var response = r as HttpWebResponse;

        if (response is not null)
        {
            CookieCollection cookies = response.Cookies;
            Pars.Cookies.Add(cookies);
        }
    }
}