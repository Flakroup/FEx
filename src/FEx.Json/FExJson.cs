using FEx.DI.Abstractions;
using FEx.Json.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace FEx.Json;

public class FExJson : InitializeModule<IFExJsonContainer>
{
    private readonly IContractResolver _contractResolver;

    public FExJson(IContractResolver contractResolver)
    {
        _contractResolver = contractResolver;
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();

        JsonExtensions.ConfigureDefaultSettings(defaultSettings =>
            defaultSettings.ContractResolver = _contractResolver);
    }

    protected override void AddServices(IFExJsonContainer container, IServiceCollection services) =>
        FExJsonModule.AddServices(container, services);
}