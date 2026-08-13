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

    // StrongInject's Resolve() returns an Owned<T> that owns the resolved instance graph; discarding it (as
    // this container used to, with IDISP004 suppressed) leaked every IDisposable resolved through the static
    // FExServiceProvider.Get<T>(). We retain each Owned and dispose it when the provider is released, so a
    // resolved disposable is freed on shutdown instead of never. Guarded by a lock - resolutions can run on
    // background threads. (Async resolutions via IAsyncContainer are not tracked; FEx containers are sync.)
    private readonly List<IDisposable> _resolvedOwneds = [];
    private readonly object _resolvedOwnedsLock = new();

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
#pragma warning disable IDISP004 // the Owned is retained in _resolvedOwneds and disposed on Release
            return TrackOwned(container.Resolve<T>());
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
#pragma warning disable IDISP004 // the Owned is retained in _resolvedOwneds and disposed on Release
            return TrackOwned(container.Resolve<T[]>());
#pragma warning restore IDISP004

        throw new InvalidOperationException($"Couldn't resolve collection of type: {typeof(T).FullName}");
    }

    public IEnumerable<T> TryResolveServices<T>()
    {
        if (Container is IContainer<T[]> container)
        {
#pragma warning disable IDISP004 // the Owned is retained in _resolvedOwneds and disposed on Release
            return TrackOwned(container.Resolve<T[]>());
#pragma warning restore IDISP004
        }

        return [];
    }

    // Retains the Owned so its resolved instance graph is disposed on Release, and returns the instance.
    // (Resolving after Release cannot reach here - Container throws on the nulled backing field first.)
    private T TrackOwned<T>(Owned<T> owned)
    {
        lock (_resolvedOwnedsLock)
            _resolvedOwneds.Add(owned);

        return owned.Value;
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
        DisposeResolvedOwneds();
#pragma warning disable IDISP007 // Don't dispose injected
        _container?.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected
        _container = null;
    }

    private void DisposeResolvedOwneds()
    {
        IDisposable[] owneds;
        lock (_resolvedOwnedsLock)
        {
            owneds = [.. _resolvedOwneds];
            _resolvedOwneds.Clear();
        }

        foreach (var owned in owneds)
#pragma warning disable IDISP007 // we own these Owned handles (created in TrackOwned via Resolve)
            owned.Dispose();
#pragma warning restore IDISP007
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