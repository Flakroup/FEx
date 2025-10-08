using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.DependencyInjection;

public class FExServiceContainer : IFExServiceContainer
{
    private IDisposable _container;
    private bool _isDisposed;

    public void RegisterServices<TContainer>(TContainer container, IServiceCollection services = null)
        where TContainer : class, IDisposable
    {
        if (_container is not null)
            throw new InvalidOperationException("Provider is already configured");

        _container = container;

        foreach (IFExInitializable initializable in TryResolveServices<IFExInitializable>())
            initializable.Initialize();

        if (services is not null)
            foreach (IMicrosoftDIConfigurator configurator in TryResolveServices<IMicrosoftDIConfigurator>()
                         .OrderBy(static configurator => configurator.Priority))
                configurator.RegisterServicesUsingContainer(services, container);
    }

    public T ResolveService<T>()
    {
        if (_container is IContainer<T> container)
#pragma warning disable IDISP004
            return container.Resolve<T>().Value;
#pragma warning restore IDISP004

        throw new InvalidOperationException($"Couldn't resolve type: {typeof(T).FullName}");
    }

    public T ResolveOrDefault<T>(T fallback = default) =>
        _container is IContainer<T>
            ? ResolveService<T>()
            : fallback;

    public IEnumerable<T> ResolveServices<T>()
    {
        if (_container is IContainer<T[]> container)
#pragma warning disable IDISP004
            return container.Resolve<T[]>().Value;
#pragma warning restore IDISP004

        throw new InvalidOperationException($"Couldn't resolve collection of type: {typeof(T).FullName}");
    }

    public IEnumerable<T> TryResolveServices<T>()
    {
        if (_container is IContainer<T[]> container)
        {
#pragma warning disable IDISP004
            return container.Resolve<T[]>().Value;
#pragma warning restore IDISP004
        }

        return Enumerable.Empty<T>();
    }

    public async Task<T> ResolveServiceAsync<T>() =>
        // ReSharper disable once SuspiciousTypeConversion.Global
        _container is IAsyncContainer<T> container
            ? (await container.ResolveAsync<T>()).Value
            : ResolveService<T>();

    public async Task<IEnumerable<T>> ResolveServicesAsync<T>() =>
        // ReSharper disable once SuspiciousTypeConversion.Global
        _container is IAsyncContainer<T[]> container
            ? (await container.ResolveAsync<T[]>()).Value
            : ResolveServices<T>();

    public void Release()
    {
#pragma warning disable IDISP007 // Don't dispose injected
        _container?.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected
        _container = null;
    }

    #region IDisposable
    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            Release();

        _isDisposed = true;
    }

    public void Dispose() => Dispose(true);
    #endregion
}