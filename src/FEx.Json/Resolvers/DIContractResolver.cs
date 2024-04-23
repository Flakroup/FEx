using FEx.Fundamentals;
using FEx.Json.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using System;

namespace FEx.Json.Resolvers;

public class DIContractResolver : DefaultContractResolver
{
    private readonly IDIMeta _diMeta;
    private static IServiceProvider Sp => Foundation.ServiceProvider;

    public DIContractResolver(IDIMeta diMeta)
    {
        _diMeta = diMeta;
    }

    protected override JsonObjectContract CreateObjectContract(Type objectType)
    {
        if (objectType is not null
            && _diMeta.IsRegistred(objectType))
        {
            JsonObjectContract contract = DIResolveContract(objectType);
            contract.DefaultCreator = () => Sp.GetRequiredService(objectType);

            return contract;
        }

        return base.CreateObjectContract(objectType);
    }

    private JsonObjectContract DIResolveContract(Type objectType)
    {
        Type fType = _diMeta.RegistredTypeFor(objectType);

        return fType is not null
            ? base.CreateObjectContract(fType)
            : CreateObjectContract(objectType);
    }
}