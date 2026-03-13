using JetBrains.Annotations;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;

namespace FEx.Agnostics.Abstractions.Models;

public class WebRequestParams
{
    public ICredentials Credentials { get; set; }
    public string UserAgent { get; set; }
    public IDictionary<string, string> Headers { get; set; }
    public CookieContainer Cookies { get; set; }
    public string Method { get; set; }
    public int? Timeout { get; set; }
    public bool? Pipelined { get; set; }
    public bool? KeepAlive { get; set; }
    public int? ReadWriteTimeout { get; set; }

    [CanBeNull]
    public IWebProxy Proxy { get; set; }

    public bool IsProxyNull { get; set; }
    public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

    public WebRequestParams(IEnumerable<Cookie> cookies = null)
    {
        IsProxyNull = false;
        Proxy = null;
        ReadWriteTimeout = null;
        KeepAlive = null;
        Pipelined = null;
        Timeout = null;
        Method = null;
        Cookies = null;
        Headers = null;
        UserAgent = null;
        Credentials = null;

        if (cookies is not null)
        {
            Cookies = new();

            foreach (var c in cookies)
                Cookies.Add(c);
        }
    }
}