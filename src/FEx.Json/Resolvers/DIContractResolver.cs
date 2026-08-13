using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Json.Helpers;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

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

            contract.DefaultCreator = () =>
            {
                var method = typeof(IFExServiceContainer)
                    .GetMethod(nameof(IFExServiceContainer.ResolveService),
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    ?.MakeGenericMethod(objectType);

                // objectType is registered (checked above), so ResolveService exists and
                // returns a non-null service instance.
                return method?.Invoke(FExServiceProvider.ServiceContainer, null)!;
            };

            return contract;
        }

        return base.CreateObjectContract(objectType);
    }

    private JsonObjectContract DIResolveContract(Type objectType)
    {
        var fType = _diMeta.RegistredTypeFor(objectType);

        return fType is not null
            ? base.CreateObjectContract(fType)
            : CreateObjectContract(objectType);
    }
}