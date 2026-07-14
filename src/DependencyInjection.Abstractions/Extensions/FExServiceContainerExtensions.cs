using FEx.DependencyInjection.Abstractions.Interfaces;
using System;

namespace FEx.DependencyInjection.Abstractions.Extensions;

public static class FExServiceContainerExtensions
{
    public static void RegisterServices<TContainer>(this IFExServiceContainer container, TContainer diContainer)
        where TContainer : class, IDisposable =>
        container.RegisterServices(diContainer, null);

    public static T? ResolveOrDefault<T>(this IFExServiceContainer container) => container.ResolveOrDefault(default(T));
}