using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Json.Abstractions.Helpers;

public sealed class DIMeta : InitializeOnlyModule
{
    private readonly Dictionary<string, Type?> _register;
    private readonly Dictionary<string, ServiceLifetime> _lifetimes;

    public DIMeta()
    {
        _register = [];
        _lifetimes = [];
    }

    public override async ValueTask OnCompleteInitializationAsync(IServiceCollection services)
    {
        await base.OnCompleteInitializationAsync(services);
        ProcessRegisteredServices(services);
    }

    public bool IsRegistered(Type t) => t is not null && _register.ContainsKey(t.FullName!);

    public bool IsTransient(Type t) =>
        t.FullName is { } key && _lifetimes.TryGetValue(key, out var lifetime) && lifetime == ServiceLifetime.Transient;

    public Type? RegisteredTypeFor(Type? t)
    {
        var key = t?.FullName;

        return key is not null && _register.TryGetValue(key, out var value)
            ? value
            : t;
    }

    protected override void RegisterServices(object? container, IServiceCollection services) =>
        ProcessRegisteredServices(services);

    private void ProcessRegisteredServices(IServiceCollection services)
    {
        foreach (var s in services)
        {
            _register[s.ServiceType.FullName!] = s.ImplementationType;
            _lifetimes[s.ServiceType.FullName!] = s.Lifetime;
        }
    }
}