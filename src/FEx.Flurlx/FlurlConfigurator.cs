using FEx.Agnostics.Abstractions;
using FEx.Flurlx.Abstractions.Interfaces;
using FEx.Flurlx.Services;
using FEx.Json.Extensions;
using FEx.Logging.Abstractions.Interfaces;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Newtonsoft;
using Polly;
using System.Net.Http;

namespace FEx.Flurlx;

public class FlurlConfigurator : FExInitialize, IFlurlConfigurator
{
    private readonly IApiConfiguration _apiConfiguration;
    private readonly IFlurlClientCache _flurlClientCache;
    private readonly ILoggable _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public FlurlConfigurator(IApiConfiguration apiConfiguration,
                             IFlurlClientCache flurlClientCache,
                             ILoggable logger = null)
    {
        _apiConfiguration = apiConfiguration;
        _flurlClientCache = flurlClientCache;
        _logger = logger;

        // Build Polly policy from configuration
        var policyBuilder = new FExPollyPolicyBuilder(_logger);
        _resiliencePolicy = policyBuilder.BuildFullSuitePolicy(_apiConfiguration.PollyConfig);

        FlurlHttp.Clients.WithDefaults(DefaultClientConfiguration);
        _flurlClientCache.Add(_apiConfiguration.ClientName, _apiConfiguration.BaseUrl, DefaultClientConfiguration);
    }

    public IFlurlClient GetClient() => _flurlClientCache.Get(_apiConfiguration.ClientName);

    public IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy() => _resiliencePolicy;

    private void DefaultClientConfiguration(IFlurlClientBuilder builder)
    {
        builder.Settings.JsonSerializer = new NewtonsoftJsonSerializer(JsonExtensions.DefaultSettings);
        builder.Settings.Timeout = _apiConfiguration.PollyConfig.RequestTimeout;

        if (_apiConfiguration.IgnoreSSLErrors)
            builder.ConfigureInnerHandler(handler =>
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true);
    }
}