using FEx.Agnostics.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.DependencyInjection;

[Register(typeof(FExServiceContainer), Scope.SingleInstance, typeof(IFExServiceContainer))]
[Register(typeof(FExDependencyInjectionModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExDependencyInjectionModule : InitializeModule<IFExDependencyInjectionContainer, IServiceCollection>
{
    [Instance(Options.AsEverythingPossible)]
    public static FExStrongInjectServiceProvider ServiceProvider { get; } = new();

    protected override void RegisterServices(IFExDependencyInjectionContainer? container, IServiceCollection services)
    {
        container.Guard(nameof(container));

        services.AddSingletonServiceUsingContainer<IFExServiceContainer>(container);

        services.AddTransientServiceUsingContainer<IAsyncConfigurator[]>(container);
        services.AddTransientServiceUsingContainer<IConfigurator[]>(container);
    }
}