using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Json.Helpers;

public sealed class DIMeta : InitializeOnlyModule
{
    private readonly Dictionary<string, Type> _register;

    public DIMeta()
    {
        _register = [];
    }

    public override async Task OnCompleteInitializationAsync(IServiceCollection services)
    {
        await base.OnCompleteInitializationAsync(services);
        RegisterServices(services);
    }

    public bool IsRegistred(Type t) => t is not null && _register.ContainsKey(t.FullName!);

    public Type RegistredTypeFor(Type t)
    {
        string key = t?.FullName;

        return key is not null && _register.TryGetValue(key, out Type value)
            ? value
            : t;
    }

    protected override void OnInitialize()
    {
    }

    protected override void AddServices(object container, IServiceCollection services) => RegisterServices(services);

    private void RegisterServices(IServiceCollection services)
    {
        foreach (ServiceDescriptor s in services)
            _register[s.ServiceType.FullName!] = s.ImplementationType;
    }
}