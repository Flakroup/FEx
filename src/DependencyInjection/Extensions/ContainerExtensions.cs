using FEx.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FEx.DependencyInjection.Extensions;

public static class ContainerExtensions
{
    public static IServiceCollection EnsureContainerSingleton<TContainer>(
        this IServiceCollection services,
        IFExServiceProvider serviceProvider) where TContainer : class
    {
        services.TryAddSingleton(serviceProvider.GetContainer<TContainer>());

        return services;
    }
}