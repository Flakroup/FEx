using StrongInject;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IFExServiceProvider : IServiceProvider, IDisposable
{
    T GetRequiredService<T>();
    T TryResolveService<T>();
    T GetRequiredService<T>(Type serviceType);
    object GetRequiredService(Type serviceType);
    TContainer GetContainer<TContainer>() where TContainer : class;
    IScopeProvider CreateScope();
    T GetInstance<T>();
    object GetInstance(Type serviceType);

    /// <summary>
    /// Configure the service provider for external DI engine integration.
    /// Only called when external DI is opt-in enabled.
    /// </summary>
    ValueTask ConfigureServiceProviderAsync();

    IEnumerable<T> TryResolveServices<T>();
}