using System.Collections.Generic;
using System.Net;

namespace FEx.Extensions.Web;

public static class WebRequestExtensions
{
    public static void PrepareRequest(this HttpWebRequest req, WebRequestParams pars)
    {
        if (pars.Credentials is not null)
            req.Credentials = pars.Credentials;

        if (pars.UserAgent is not null)
            req.UserAgent = pars.UserAgent;

        if (pars.Headers is not null)
            foreach (KeyValuePair<string, string> header in pars.Headers)
                req.Headers.Add(header.Key, header.Value);

        if (pars.Cookies is not null)
            req.CookieContainer = pars.Cookies;

        if (pars.Method is not null)
            req.Method = pars.Method;

        if (pars.Timeout is not null)
            req.Timeout = pars.Timeout.Value;

        if (pars.Pipelined.HasValue)
            req.Pipelined = pars.Pipelined.Value;

        if (pars.KeepAlive.HasValue)
            req.KeepAlive = pars.KeepAlive.Value;

        if (pars.ReadWriteTimeout.HasValue)
            req.ReadWriteTimeout = pars.ReadWriteTimeout.Value;

        if (pars.Proxy is not null)
            req.Proxy = pars.Proxy;

        if (pars.IsProxyNull)
            req.Proxy = null;

        if (pars.ServerCertificateValidationCallback is not null)
            req.ServerCertificateValidationCallback = pars.ServerCertificateValidationCallback;
    }

    public static void PrepareRequest(this WebRequest req, WebRequestParams pars)
    {
        if (pars.Credentials is not null)
            req.Credentials = pars.Credentials;

        if (pars.Headers is not null)
            foreach (KeyValuePair<string, string> header in pars.Headers)
                req.Headers.Add(header.Key, header.Value);

        if (pars.Method is not null)
            req.Method = pars.Method;

        if (pars.Timeout is not null)
            req.Timeout = pars.Timeout.Value;
    }
}