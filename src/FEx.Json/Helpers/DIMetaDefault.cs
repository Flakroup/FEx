using FEx.Extensions.Collections.Enumerables;
using FEx.Json.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace FEx.Json.Helpers;

public class DIMetaDefault : IDIMeta
{
    private readonly IDictionary<Type, Type> _register = new Dictionary<Type, Type>();

    public DIMetaDefault(IServiceCollection services)
    {
        foreach (ServiceDescriptor s in services)
            _register[s.ServiceType] = s.ImplementationType;
    }

    public bool IsRegistred(Type t) => t is not null && _register.ContainsKey(t);

    public Type RegistredTypeFor(Type t)
    {
        Type key = _register.Keys.FindInEnumerable(k => k.FullName == t.FullName);

        return key is not null
            ? _register[key] is not null
                ? _register[key]
                : key
            : null;
    }
}