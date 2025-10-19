using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Flurlx.Abstractions.Interfaces;
using Flurl.Http.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Flurlx;

[Register(typeof(FlurlConfigurator), Scope.SingleInstance, typeof(IFlurlConfigurator))]
[Register(typeof(FlurlClientCache), Scope.SingleInstance, typeof(IFlurlClientCache))]
[Register(typeof(FExFlurlx), Scope.SingleInstance, typeof(FExFlurlx), typeof(IFExInitialize))]
[Register(typeof(FExFlurlxModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExFlurlxModule : InitializeModule<IFExFlurlxContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExFlurlxContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IFlurlConfigurator>(container);
        services.AddSingletonServiceUsingContainer<IFlurlClientCache>(container);

        // Register Polly policy as factory from configurator
        services.AddSingleton(sp =>
        {
            IFlurlConfigurator configurator = sp.GetRequiredService<IFlurlConfigurator>();

            return configurator.GetResiliencePolicy();
        });
    }
}