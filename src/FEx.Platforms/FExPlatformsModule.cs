using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Platforms.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Platforms;

[Register(typeof(RegistryService), Scope.SingleInstance, typeof(IRegistryService))]
[Register(typeof(FExPlatforms), Scope.SingleInstance, typeof(FExPlatforms), typeof(IFExInitialize))]
[Register(typeof(FExPlatformsModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExPlatformsModule : InitializeModule<IFExPlatformsContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExPlatformsContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IRegistryService>(container);
    }
}