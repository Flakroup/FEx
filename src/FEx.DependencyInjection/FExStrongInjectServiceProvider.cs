using FEx.Abstractions;
using FEx.Fundamentals;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FEx.DependencyInjection;

public class FExStrongInjectServiceProvider : IFExServiceProvider
{
    private readonly MethodInfo _method;
    private object _provider;

    public FExStrongInjectServiceProvider()
    {
        _method = GetType()
            .GetMethods()
            .Single(x => x.IsGenericMethod && x.Name == nameof(GetRequiredService) && x.GetParameters().Length == 0);
    }

    public T GetRequiredService<T>()
    {
        if (_provider is not IContainer<T> container)
            throw new InvalidOperationException($"Couldn't resolve type: {typeof(T).FullName}");
        return container.Resolve<T>().Value;
    }

    public T TryResolveService<T>() =>
        _provider is IContainer<T> container
            ? container.Resolve<T>().Value
            : default;

    public T GetRequiredService<T>(Type serviceType)
    {
        MethodInfo generic = _method.MakeGenericMethod(serviceType);
        return (T)generic.Invoke(this, null);
    }

    public object GetRequiredService(Type serviceType)
    {
        MethodInfo generic = _method.MakeGenericMethod(serviceType);
        return generic.Invoke(this, null);
    }

    public IServiceScope CreateScope() => null;

    public object GetService(Type serviceType) => null;

    public TContainer ConfigureServiceProvider<TContainer>() where TContainer : class, IContainer<Foundation>, new()
    {
        _provider = new TContainer();
        return (TContainer)_provider;
    }

    public async Task<T> GetRequiredServiceAsync<T>() =>
        _provider is IAsyncContainer<T> container
            ? (await container.ResolveAsync<T>()).Value
            : GetRequiredService<T>();
}