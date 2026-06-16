using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Flurlx.Abstractions.Interfaces;
using FEx.Flurlx.Services;
using Flurl.Http.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Flurlx;

/// <summary>
/// Serializer-agnostic FEx.Flurlx registrations (client, cache, resilience, configurator).
/// Pick a serializer by registering a serializer module on top: the core
/// <see cref="FExFlurlxModule"/> (System.Text.Json, default) or the opt-in
/// FEx.Flurlx.Newtonsoft module. Both supply an <see cref="ISerializer"/> via factory.
/// </summary>
[Register(typeof(FlurlConfigurator), Scope.SingleInstance, typeof(IFlurlConfigurator))]
[Register(typeof(FlurlClientCache), Scope.SingleInstance, typeof(IFlurlClientCache))]
[Register(typeof(FExPollyPolicyBuilder), Scope.SingleInstance, typeof(IFExPollyPolicyBuilder))]
[Register(typeof(FExFlurlx), Scope.SingleInstance, typeof(FExFlurlx), typeof(IFExInitializable))]
[Register(typeof(FExFlurlxBaseModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExFlurlxBaseModule : InitializeModule<IFExFlurlxContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExFlurlxContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IFlurlConfigurator>(container);
        services.AddSingletonServiceUsingContainer<IFlurlClientCache>(container);
    }
}
