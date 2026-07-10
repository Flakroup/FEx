using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Platforms.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Platforms;

[Register(typeof(RegistryService), Scope.SingleInstance, typeof(IRegistryService))]
[Register(typeof(FExPlatforms), Scope.SingleInstance, typeof(FExPlatforms), typeof(IFExInitializable))]
[Register(typeof(FExPlatformsModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExPlatformsModule : InitializeModule<IFExPlatformsContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExPlatformsContainer? container, IServiceCollection services)
    {
        container = container.Guard(nameof(container));
        services.AddSingletonServiceUsingContainer<IRegistryService>(container);
    }
}