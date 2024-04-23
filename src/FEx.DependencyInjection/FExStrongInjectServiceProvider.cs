using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FEx.DependencyInjection;

public sealed class FExStrongInjectServiceProvider : IFExServiceProvider
{
    private readonly MethodInfo _method;
    private IDisposable _provider;

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

#pragma warning disable IDISP004
        return container.Resolve<T>().Value;
#pragma warning restore IDISP004
    }

#pragma warning disable IDISP004
    public T TryResolveService<T>() =>
        _provider is not IContainer<T> container
            ? default
            : container.Resolve<T>().Value;
#pragma warning restore IDISP004

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

    public TContainer GetContainer<TContainer>() where TContainer : class => (TContainer)_provider;

    public IServiceScope CreateScope() => default;

    public object GetService(Type serviceType)
    {
        try
        {
            return GetRequiredService(serviceType);
        }
        catch
        {
            //ignored
            return default;
        }
    }

    public TContainer ConfigureServiceProvider<TContainer>() where TContainer : class, IDisposable, new()
    {
        _provider?.Dispose();
        _provider = new TContainer();
        IInitializeModule[] modules = TryResolveService<IInitializeModule[]>();

        if (modules?.Length > 0)
            foreach (IInitializeModule initializer in modules)
                initializer.Initialize();

        return (TContainer)_provider;
    }

#pragma warning disable IDE0079
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
#pragma warning restore IDE0079
    public async Task<T> GetRequiredServiceAsync<T>() =>
        _provider is IAsyncContainer<T> container
            ? (await container.ResolveAsync<T>()).Value
            : GetRequiredService<T>();

    #region IDisposable
    public void Dispose() => _provider?.Dispose();
    #endregion
}