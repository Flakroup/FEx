using FEx.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using System;
using System.Threading.Tasks;

namespace FEx.DependencyInjection;

public class FExStrongInjectServiceProvider : IFExServiceProvider
{
    private object _provider;

    public T GetRequiredService<T>() =>
        _provider is not IContainer<T> container
            ? throw new InvalidOperationException($"Couldn't resolve type: {typeof(T).FullName}")
            : container.Resolve<T>().Value;

    public T TryResolveService<T>() =>
        _provider is IContainer<T> container
            ? container.Resolve<T>().Value
            : default;

    public T GetRequiredService<T>(Type serviceType) => default;

    public object GetRequiredService(Type serviceType) => null;

    public IServiceScope CreateScope() => null;

    public object GetService(Type serviceType) => null;

    public void ConfigureServiceProvider<TContainer>() where TContainer : class, new()
    {
        _provider = new TContainer();
    }

    public async Task<T> GetRequiredServiceAsync<T>() =>
        _provider is IAsyncContainer<T> container
            ? (await container.ResolveAsync<T>()).Value
            : GetRequiredService<T>();
}