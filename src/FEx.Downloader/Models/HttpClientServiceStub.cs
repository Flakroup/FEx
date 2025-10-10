using FEx.Agnostics.Abstractions.Models;
using System;

namespace FEx.Downloader.Models;

public class HttpClientServiceStub : IEquatable<HttpClientServiceStub>
{
    public string Host { get; }
    public WebRequestParams Pars { get; set; }
    public int HttpClientsCount { get; set; }

    public HttpClientServiceStub(Uri url, WebRequestParams pars = null, int clientsCount = 2)
        : this(url.Host, pars, clientsCount)
    {
    }

    public HttpClientServiceStub(string urlHost, WebRequestParams pars = null, int clientsCount = 2)
    {
        Host = urlHost;
        Pars = pars;

        HttpClientsCount = clientsCount > 0
            ? clientsCount
            : 2;
    }

    public bool Equals(HttpClientServiceStub other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other)
               || string.Equals(Host, other.Host, StringComparison.InvariantCultureIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((HttpClientServiceStub)obj);
    }

    public override int GetHashCode() =>
        Host is not null
            ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(Host)
            : 0;
}