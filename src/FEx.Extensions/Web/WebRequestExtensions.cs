using System.Collections.Generic;
using System.Net;

namespace FEx.Extensions.Web;

public static class WebRequestExtensions
{
    public static void PrepareRequest(this HttpWebRequest req, WebRequestParams pars)
    {
        if (pars.Credentials != null)
            req.Credentials = pars.Credentials;

        if (pars.UserAgent != null)
            req.UserAgent = pars.UserAgent;

        if (pars.Headers != null)
            foreach (KeyValuePair<string, string> header in pars.Headers)
                req.Headers.Add(header.Key, header.Value);

        if (pars.Cookies != null)
            req.CookieContainer = pars.Cookies;

        if (pars.Method != null)
            req.Method = pars.Method;

        if (pars.Timeout != null)
            req.Timeout = pars.Timeout.Value;

        if (pars.Pipelined.HasValue)
            req.Pipelined = pars.Pipelined.Value;

        if (pars.KeepAlive.HasValue)
            req.KeepAlive = pars.KeepAlive.Value;

        if (pars.ReadWriteTimeout.HasValue)
            req.ReadWriteTimeout = pars.ReadWriteTimeout.Value;

        if (pars.Proxy != null)
            req.Proxy = pars.Proxy;

        if (pars.IsProxyNull)
            req.Proxy = null;

        if (pars.ServerCertificateValidationCallback != null)
            req.ServerCertificateValidationCallback = pars.ServerCertificateValidationCallback;
    }

    public static void PrepareRequest(this WebRequest req, WebRequestParams pars)
    {
        if (pars.Credentials != null)
            req.Credentials = pars.Credentials;

        if (pars.Headers != null)
            foreach (KeyValuePair<string, string> header in pars.Headers)
                req.Headers.Add(header.Key, header.Value);

        if (pars.Method != null)
            req.Method = pars.Method;

        if (pars.Timeout != null)
            req.Timeout = pars.Timeout.Value;
    }
}