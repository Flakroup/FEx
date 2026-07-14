using FEx.Agnostics.Abstractions.Extensions;
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
    private IDisposable? _container;

    private IDisposable Container => _container.GuardProperty();

    private bool _isDisposed;

    public void RegisterServices<TContainer>(TContainer container, IServiceCollection? services)
        where TContainer : class, IDisposable
    {
        if (_container is not null)
            throw new InvalidOperationException("Provider is already configured");

        _container = container;

        Initialize();

        if (services is not null)
            foreach (var configurator in TryResolveServices<IMicrosoftDIConfigurator>()
                         .OrderBy(static configurator => configurator.Priority))
                configurator.RegisterServicesUsingContainer(services, container);
    }

    public T ResolveService<T>()
    {
        if (Container is IContainer<T> container)
#pragma warning disable IDISP004
            return container.Resolve<T>().Value;
#pragma warning restore IDISP004

        throw new InvalidOperationException($"Couldn't resolve type: {typeof(T).FullName}");
    }

    public T? ResolveOrDefault<T>(T? fallback) =>
        Container is IContainer<T>
            ? ResolveService<T>()
            : fallback;

    public IEnumerable<T> ResolveServices<T>()
    {
        if (Container is IContainer<T[]> container)
#pragma warning disable IDISP004
            return container.Resolve<T[]>().Value;
#pragma warning restore IDISP004

        throw new InvalidOperationException($"Couldn't resolve collection of type: {typeof(T).FullName}");
    }

    public IEnumerable<T> TryResolveServices<T>()
    {
        if (Container is IContainer<T[]> container)
        {
#pragma warning disable IDISP004
            return container.Resolve<T[]>().Value;
#pragma warning restore IDISP004
        }

        return [];
    }

    public async Task<T> ResolveServiceAsync<T>() =>
        // ReSharper disable once SuspiciousTypeConversion.Global
        Container is IAsyncContainer<T> container
            ? (await container.ResolveAsync<T>()).Value
            : ResolveService<T>();

    public async Task<IEnumerable<T>> ResolveServicesAsync<T>() =>
        // ReSharper disable once SuspiciousTypeConversion.Global
        Container is IAsyncContainer<T[]> container
            ? (await container.ResolveAsync<T[]>()).Value
            : ResolveServices<T>();

    public void Release()
    {
#pragma warning disable IDISP007 // Don't dispose injected
        _container?.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected
        _container = null;
    }

    private void Initialize()
    {
        ICollection<IFExPriorityInitialize> priorityInitializers =
            [.. TryResolveServices<IFExPriorityInitialize>().OrderBy(static initializer => initializer.Priority)];
        priorityInitializers.InitializeAll();

        ICollection<IFExInitializable> initializers = [.. TryResolveServices<IFExInitializable>()];
        initializers.InitializeAll();

        // Note: Engine-specific modules (IInitializeModule<TEngineContext>) are handled by 
        // their respective providers during ConfigureServiceProviderAsync, not here.
        _ = TryResolveServices<IInitializeModule<IServiceCollection>>().ToArray();
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