using FEx.DI.Abstractions;
using FEx.Json.Helpers;
using Newtonsoft.Json.Serialization;
using System;

namespace FEx.Json.Resolvers;

public class DIContractResolver : DefaultContractResolver
{
    private readonly DIMeta _diMeta;

    public DIContractResolver(DIMeta diMeta)
    {
        _diMeta = diMeta;
    }

    protected override JsonObjectContract CreateObjectContract(Type objectType)
    {
        if (_diMeta.IsRegistred(objectType))
        {
            JsonObjectContract contract = DIResolveContract(objectType);
            contract.DefaultCreator = () => FExServiceProvider.Get(objectType);

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