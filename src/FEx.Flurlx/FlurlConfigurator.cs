using FEx.Agnostics.Abstractions;
using FEx.Flurlx.Abstractions.Interfaces;
using FEx.Flurlx.Services;
using FEx.Json.Extensions;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using Polly;

namespace FEx.Flurlx;

public class FlurlConfigurator : FExInitializable, IFlurlConfigurator
{
    private readonly IApiConfiguration _apiConfiguration;
    private readonly IFlurlClientCache _flurlClientCache;
    private readonly IAsyncPolicy<IFlurlResponse> _resiliencePolicy;

    public FlurlConfigurator(IApiConfiguration apiConfiguration,
                             IFlurlClientCache flurlClientCache,
                             IFExPollyPolicyBuilder policyBuilder)
    {
        _apiConfiguration = apiConfiguration;
        _flurlClientCache = flurlClientCache;

        // Build Polly policy from configuration
        _resiliencePolicy = policyBuilder.BuildFullSuitePolicy(_apiConfiguration.PollyConfig);

        FlurlHttp.Clients.WithDefaults(DefaultClientConfiguration);
        _flurlClientCache.Add(_apiConfiguration.ClientName, _apiConfiguration.BaseUrl, DefaultClientConfiguration);
    }

    public IFlurlClient GetClient() => _flurlClientCache.Get(_apiConfiguration.ClientName);

    public IAsyncPolicy<IFlurlResponse> GetResiliencePolicy() => _resiliencePolicy;

    private void DefaultClientConfiguration(IFlurlClientBuilder builder)
    {
        builder.Settings.JsonSerializer = new NewtonsoftJsonSerializer(JsonExtensions.DefaultSettings);
        builder.Settings.Timeout = _apiConfiguration.PollyConfig.RequestTimeout;

        if (_apiConfiguration.IgnoreSSLErrors)
            builder.ConfigureInnerHandler(handler =>
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true);
    }
}