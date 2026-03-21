using FEx.DependencyInjection.Abstractions.Interfaces;

namespace FEx.DependencyInjection.Abstractions.Extensions;

public static class FExServiceContainerExtensions
{
    public static void RegisterServices<TContainer>(this IFExServiceContainer container, TContainer diContainer)
        where TContainer : class, System.IDisposable =>
        container.RegisterServices(diContainer, null);

    public static T ResolveOrDefault<T>(this IFExServiceContainer container) =>
        container.ResolveOrDefault(default(T));
}
