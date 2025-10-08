using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Basics;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.DependencyInjection.Abstractions;

public class FExServiceProvider : IFExServiceProvider
{
    /// <summary>
    /// Lock object for thread-safe initialization.
    /// </summary>
    private static readonly object _initializationLock = new();

    /// <summary>
    /// Tracks the container instance for idempotent initialization.
    /// </summary>
    private static IDisposable _containerInstance;

    public static FExServiceProvider Instance => new();

    /// <summary>
    /// Retrieves the <see cref="IFExServiceContainer" /> instance.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static IFExServiceContainer ServiceContainer { get; private set; }

    /// <summary>
    /// Static reference to the current global service provider for multi-DI coordination.
    /// </summary>
    private static IFExServiceProvider ServiceProvider { get; set; }

    static FExServiceProvider()
    {
        ServiceProvider = Instance;
        StaticsBase.ServiceProvider = Instance;
    }

    private FExServiceProvider()
    {
    }

    /// <summary>
    /// Retrieves the <see cref="T" /> instance.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public T GetInstance<T>() => Get<T>();

    /// <summary>
    /// Retrieves the instance of the specified type.
    /// </summary>
    public object GetInstance(Type serviceType) => ServiceContainer.ResolveService<object>();

    /// <summary>
    /// Gets the required service object of the specified type.
    /// </summary>
    public T GetRequiredService<T>() => ServiceContainer.ResolveService<T>();

    /// <summary>
    /// Gets the required service object of the specified type.
    /// </summary>
    public T GetRequiredService<T>(Type serviceType) => (T)ServiceContainer.ResolveService<object>();

    /// <summary>
    /// Gets the required service object of the specified type.
    /// </summary>
    public object GetRequiredService(Type serviceType) => ServiceContainer.ResolveService<object>();

    /// <summary>
    /// Tries to resolve service of the specified type.
    /// </summary>
    public T TryResolveService<T>() => ServiceContainer.ResolveOrDefault<T>();

    /// <summary>
    /// Gets the container of the specified type.
    /// </summary>
    public TContainer GetContainer<TContainer>() where TContainer : class =>
        ServiceContainer as TContainer
        ?? throw new InvalidOperationException($"Container is not of type {typeof(TContainer).Name}");

    /// <summary>
    /// Creates a scope for scoped services - not supported by StrongInject containers.
    /// </summary>
    public IScopeProvider CreateScope() =>
        throw new NotSupportedException("Scoping is handled by StrongInject containers");

    /// <summary>
    /// No-op implementation for the static provider as it delegates to the actual providers.
    /// </summary>
    public ValueTask ConfigureServiceProviderAsync() => new();

    /// <summary>
    /// Gets the service object of the specified type.
    /// </summary>
    public object GetService(Type serviceType) => ServiceContainer.ResolveOrDefault<object>();

    /// <summary>
    /// Default startup point of application. Creates DI container instance and wraps into service,
    /// that allows to distribute its registrations around the app.
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    /// <returns></returns>
    public static TContainer Initialize<TContainer>(IServiceCollection services = null,
                                                    Action<TContainer> configureContainer = null)
        where TContainer : class, IDisposable, IContainer<IFExServiceContainer>, new()
    {
        Release();
        var container = new TContainer();
        configureContainer?.Invoke(container);
#pragma warning disable IDISP004 // Don't ignore created IDisposable
        ServiceContainer = container.Resolve<IFExServiceContainer>().Value;
#pragma warning restore IDISP004 // Don't ignore created IDisposable

        ServiceContainer.RegisterServices(container, services);

        return container;
    }

    /// <summary>
    /// Retrieves the <see cref="T" /> instance.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when FExServiceProvider has not been initialized.</exception>
    public static T Get<T>()
    {
        if (ServiceContainer == null)
            throw new InvalidOperationException(
                $"FExServiceProvider not initialized. Call {nameof(Initialize)}<TContainer, TProvider>() first.");

        return ServiceContainer.ResolveService<T>();
    }

    /// <summary>
    /// Retrieves the <see cref="T" /> instance asynchronously.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when FExServiceProvider has not been initialized.</exception>
    public static async Task<T> GetAsync<T>()
    {
        if (ServiceContainer == null)
            throw new InvalidOperationException(
                $"FExServiceProvider not initialized. Call {nameof(Initialize)}<TContainer, TProvider>() first.");

        return await ServiceContainer.ResolveServiceAsync<T>();
    }

    /// <summary>
    /// Retrieves the <see cref="T" /> instances.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when FExServiceProvider has not been initialized.</exception>
    public static IEnumerable<T> GetAll<T>()
    {
        if (ServiceContainer == null)
            throw new InvalidOperationException(
                $"FExServiceProvider not initialized. Call {nameof(Initialize)}<TContainer, TProvider>() first.");

        return ServiceContainer.ResolveServices<T>();
    }

    /// <summary>
    /// Retrieves the <see cref="T" /> instances or returns empty enumerable if none found.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when FExServiceProvider has not been initialized.</exception>
    public static IEnumerable<T> TryGetAll<T>()
    {
        if (ServiceContainer == null)
            throw new InvalidOperationException(
                $"FExServiceProvider not initialized. Call {nameof(Initialize)}<TContainer, TProvider>() first.");

        return ServiceContainer.TryResolveServices<T>();
    }

    /// <summary>
    /// Retrieves the <see cref="T" /> instances asynchronously.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when FExServiceProvider has not been initialized.</exception>
    public static async Task<IEnumerable<T>> GetAllAsync<T>()
    {
        if (ServiceContainer == null)
            throw new InvalidOperationException(
                $"FExServiceProvider not initialized. Call {nameof(Initialize)}<TContainer, TProvider>() first.");

        return await ServiceContainer.ResolveServicesAsync<T>();
    }

    /// <summary>
    /// Retrieves the <see cref="T" /> instance or returns default value.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static T GetOrDefault<T>(T fallback = default) => ServiceContainer.ResolveOrDefault(fallback);

    /// <summary>
    /// Disposes the container.
    /// </summary>
    public static void Release()
    {
        ServiceContainer?.Release();
        ServiceProvider?.Dispose();
        ServiceProvider = null;
        ServiceContainer = null;
        _containerInstance = null;
    }

    public static TContainer Initialize<TContainer, TProvider>() where TContainer : class, IDisposable, new()
        where TProvider : class, IFExStrongInjectServiceProvider, new()
    {
        // Idempotent: return existing container if already initialized with same type
        if (_containerInstance is TContainer existingContainer)
            return existingContainer;

        // Thread-safety: prevent concurrent initialization
        lock (_initializationLock)
        {
            // Double-check after acquiring lock
            if (_containerInstance is TContainer existing)
                return existing;

            // Dispose previously set multi-di provider if any
            ServiceProvider?.Dispose();
            var serviceProvider = new TProvider();

            TContainer container = serviceProvider.ConfigureServiceProvider<TContainer>();
            ServiceProvider = serviceProvider;
            _containerInstance = container;

            // Set ServiceContainer by resolving from the new container
#pragma warning disable IDISP004 // Don't ignore created IDisposable
            ServiceContainer = container is IContainer<IFExServiceContainer> containerResolver
                ? containerResolver.Resolve<IFExServiceContainer>().Value
                : throw new InvalidOperationException(
                    $"{typeof(TContainer).Name} must implement IContainer<IFExServiceContainer>");
#pragma warning restore IDISP004

            // Register services with the container
            ServiceContainer.RegisterServices(container);

            InitializeInternal(serviceProvider);

            return container;
        }
    }

    public static async Task InitializeAsync<TProvider>() where TProvider : class, IFExServiceProvider
    {
        if (ServiceProvider == null
            || ServiceContainer == null)
            throw new InvalidOperationException(
                $"FExServiceProvider not initialized. Call {nameof(Initialize)}<TContainer, TProvider>() first.");

        // Resolve the external DI provider from current provider
        TProvider serviceProvider = ServiceProvider.GetInstance<TProvider>();

        await serviceProvider.ConfigureServiceProviderAsync();
        ServiceProvider = serviceProvider;
        InitializeInternal(serviceProvider);
    }

    public static TModule GetDefaultContainer<TModule>() where TModule : class
    {
        try
        {
            return ServiceProvider.GetContainer<TModule>();
        }
        catch (Exception ex)
        {
            object provider = null;

            try
            {
                provider = ServiceProvider.GetContainer<object>();
            }
            catch
            {
                // ignored
            }

            if (provider is not null)
                throw new InvalidOperationException(
                    $"Default container {provider.GetType().FullName ?? "null"} does not implement interface or type {typeof(TModule).FullName}",
                    ex);

            if (ServiceProvider is null)
                throw new InvalidOperationException("Service provider wasn't initialized");

            throw;
        }
    }

    public static async Task<IFExServiceProvider> RegisterDependenciesAsync(
        Func<IFExServiceProvider, Task> serviceProviderConfiguration)
    {
        serviceProviderConfiguration.Guard(nameof(serviceProviderConfiguration));
        IFExServiceProvider provider = ServiceProvider;
        await serviceProviderConfiguration(provider);

        return provider;
    }

    private static void InitializeInternal(IFExServiceProvider serviceProvider)
    {
        var priorityInitializers = (serviceProvider.TryResolveService<IFExPriorityInitialize[]>() ?? [])
            .OrderBy(initializer => initializer.Priority)
            .ToList();

        priorityInitializers.InitializeAll();

        IFExInitialize[] initializers = serviceProvider.TryResolveService<IFExInitialize[]>() ?? [];
        initializers.InitializeAll();

        // Note: Engine-specific modules (IInitializeModule<TEngineContext>) are handled by 
        // their respective providers during ConfigureServiceProviderAsync, not here.
    }

    #region IDisposable
    /// <summary>
    /// Disposes the service provider.
    /// </summary>
    public void Dispose() => ServiceContainer?.Dispose();
    #endregion
}