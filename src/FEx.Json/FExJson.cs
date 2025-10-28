using FEx.Agnostics.Abstractions;
using FEx.Json.Extensions;
using Newtonsoft.Json.Serialization;

namespace FEx.Json;

public class FExJson : FExInitializable
{
    private readonly IContractResolver _contractResolver;

    public FExJson(IContractResolver contractResolver)
    {
        _contractResolver = contractResolver;
    }

    protected override void OnInitialize()
    {
        JsonExtensions.ConfigureDefaultSettings(defaultSettings =>
            defaultSettings.ContractResolver = _contractResolver);
    }
}