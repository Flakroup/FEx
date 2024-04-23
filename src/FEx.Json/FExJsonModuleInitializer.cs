using FEx.DependencyInjection.Abstractions;
using FEx.Json.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace FEx.Json;

public class FExJsonModuleInitializer : InitializeModule<IFExJsonModule>
{
    private readonly IContractResolver _contractResolver;

    public FExJsonModuleInitializer(IContractResolver contractResolver)
    {
        _contractResolver = contractResolver;
    }

    protected override void OnInitialize()
    {
        JsonExtensions.ConfigureDefaultSettings(defaultSettings => defaultSettings.ContractResolver = _contractResolver);
    }

    protected override void AddServices(IFExJsonModule container, IServiceCollection services)
    {
        FExJsonModule.AddServices(container, services);
    }
}