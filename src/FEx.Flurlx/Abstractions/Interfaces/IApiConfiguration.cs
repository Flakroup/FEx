using Flurl;

namespace FEx.Flurlx.Abstractions.Interfaces;

public interface IApiConfiguration
{
    Url BaseUrl { get; }
    string ClientName { get; }
    bool IgnoreSSLErrors { get; }
}