using JetBrains.Annotations;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;

namespace FEx.Agnostics.Abstractions.Models;

/// <summary>Settings for a web request that can be applied to an <c>HttpClient</c> handler or a legacy <c>WebRequest</c>; unset (null) values leave the defaults untouched.</summary>
public class WebRequestParams
{
    /// <summary>Gets or sets the credentials used to authenticate the request.</summary>
    public ICredentials? Credentials { get; set; }
    /// <summary>Gets or sets the <c>User-Agent</c> header value.</summary>
    public string? UserAgent { get; set; }
    /// <summary>Gets or sets additional request headers keyed by header name.</summary>
    public IDictionary<string, string>? Headers { get; set; }
    /// <summary>Gets or sets the container holding the cookies sent with the request.</summary>
    public CookieContainer? Cookies { get; set; }
    /// <summary>Gets or sets the HTTP method, for example <c>GET</c> or <c>POST</c>.</summary>
    public string? Method { get; set; }
    /// <summary>Gets or sets the request timeout in milliseconds.</summary>
    public int? Timeout { get; set; }
    /// <summary>Gets or sets a value indicating whether the request may be pipelined.</summary>
    public bool? Pipelined { get; set; }
    /// <summary>Gets or sets a value indicating whether the connection is kept alive.</summary>
    public bool? KeepAlive { get; set; }
    /// <summary>Gets or sets the read/write timeout in milliseconds.</summary>
    public int? ReadWriteTimeout { get; set; }

    /// <summary>Gets or sets the proxy used for the request.</summary>
    [CanBeNull]
    public IWebProxy? Proxy { get; set; }

    /// <summary>Gets or sets a value indicating whether the request must bypass any proxy, even the system one.</summary>
    public bool IsProxyNull { get; set; }
    /// <summary>Gets or sets the callback that validates the server certificate.</summary>
    public RemoteCertificateValidationCallback? ServerCertificateValidationCallback { get; set; }

    /// <summary>Initializes the parameters with every setting unset</summary>
    /// <param name="cookies">Cookies copied into a new container; no container is created when null.</param>
    public WebRequestParams(IEnumerable<Cookie>? cookies = null)
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