using FEx.DependencyInjection.Abstractions;
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
            var contract = DIResolveContract(objectType);

            // Only transient registrations yield a fresh instance per call. Handing Newtonsoft a live
            // singleton/scoped instance would let deserialization overwrite shared state.
            if (_diMeta.IsTransient(objectType))
                contract.DefaultCreator = () => FExServiceProvider.Instance.GetRequiredService(objectType);

            return contract;
        }

        return base.CreateObjectContract(objectType);
    }

    private JsonObjectContract DIResolveContract(Type objectType)
    {
        var fType = _diMeta.RegistredTypeFor(objectType);

        // Factory/instance registrations have no ImplementationType - build the contract for the
        // service type itself instead of re-entering CreateObjectContract (infinite recursion).
        return base.CreateObjectContract(fType ?? objectType);
    }
}