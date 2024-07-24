using FEx.Flurlx.Abstractions.Interfaces;
using FEx.Json.Extensions;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using System;

namespace FEx.Flurlx;

public class FlurlConfigurator : IFlurlConfigurator
{
    private readonly IApiConfiguration _apiConfiguration;
    private readonly IFlurlClientCache _flurlClientCache;

    public FlurlConfigurator(IApiConfiguration apiConfiguration, IFlurlClientCache flurlClientCache)
    {
        _apiConfiguration = apiConfiguration;
        _flurlClientCache = flurlClientCache;
    }

    public void Configure()
    {
        FlurlHttp.Clients.WithDefaults(DefaultClientConfiguration);
        _flurlClientCache.Add(_apiConfiguration.ClientName, _apiConfiguration.BaseUrl, DefaultClientConfiguration);
    }

    public IFlurlClient GetClient() => _flurlClientCache.Get(_apiConfiguration.ClientName);

    private void DefaultClientConfiguration(IFlurlClientBuilder builder)
    {
        builder.Settings.JsonSerializer = new NewtonsoftJsonSerializer(JsonExtensions.DefaultSettings);
        builder.Settings.Timeout = TimeSpan.FromSeconds(15);

        if (_apiConfiguration.IgnoreSSLErrors)
            builder.ConfigureInnerHandler(handler =>
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true);
    }
}