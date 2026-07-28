#pragma warning disable IDISP003, IDISP004, IDISP007, IDISP012, IDISP025 // Intentional DI container lifecycle patterns
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Helpers;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.DependencyInjection.Abstractions.Basics;
using FEx.DependencyInjection.Abstractions.Extensions;
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
    private static readonly FExSemaphoreSlim InitializationLock = new();

    /// <summary>
    /// Tracks the container instance for idempotent initialization.
    /// </summary>
    private static IDisposable? _containerInstance;

    public static FExServiceProvider Instance => new();

    /// <summary>
    /// Retrieves the <see cref="IFExServiceContainer" /> instance.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static IFExServiceContainer? ServiceContainer { get; private set; }

    /// <summary>
    /// Guarded accessor for <see cref="ServiceContainer" /> for members that assume it has been initialized.
    /// </summary>
    private static IFExServiceContainer RequiredServiceContainer => ServiceContainer.Guard(nameof(ServiceContainer));

    /// <summary>
    /// Static reference to the current global service provider for multi-DI coordination.
    /// </summary>
    private static IFExServiceProvider? ServiceProvider { get; set; }

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
    public object GetInstance(Type serviceType) => RequiredServiceContainer.ResolveService<object>();

    /// <summary>
    /// Gets the required service object of the specified type.
    /// </summary>
    public T GetRequiredService<T>() => RequiredServiceContainer.ResolveService<T>();

    /// <summary>
    /// Gets the required service object of the specified type.
    /// </summary>
    public T GetRequiredService<T>(Type serviceType) => (T)RequiredServiceContainer.ResolveService<object>();

    /// <summary>
    /// Gets the required service object of the specified type.
    /// </summary>
    public object GetRequiredService(Type serviceType) => RequiredServiceContainer.ResolveService<object>();

    /// <summary>
    /// Tries to resolve service of the specified type.
    /// </summary>
    public T? TryResolveService<T>() => ServiceContainer is null ? default : ServiceContainer.ResolveOrDefault<T>();

    /// <summary>
    /// Gets the container of the specified type.
    /// </summary>
    public TContainer GetContainer<TContainer>() where TContainer : class =>
        RequiredServiceContainer as TContainer
        ?? throw new InvalidOperationException($"Container is not of type {typeof(TContainer).Name}");

    /// <summary>
    /// Creates a scope for scoped services - not supported by StrongInject containers.
    /// </summary>
    public IScopeProvider CreateScope() =>
        throw new NotSupportedException("Scoping is handled by StrongInject containers");

    /// <summary>
    /// No-op implementation for the static provider as it delegates to the actual providers.
    /// </summary>
    public ValueTask ConfigureServiceProviderAsync() => FExValueTaskHelper.CompletedTask;

    /// <inheritdoc />
    public IEnumerable<T> TryResolveServices<T>() => RequiredServiceContainer.TryResolveServices<T>();

    /// <summary>
    /// Gets the service object of the specified type.
    /// </summary>
    public object? GetService(Type serviceType) => ServiceContainer?.ResolveOrDefault<object>();

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
                $"FExServiceProvider not initialized. Call {nameof(InitializeAsync)}<TContainer>() first.");

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
                $"FExServiceProvider not initialized. Call {nameof(InitializeAsync)}<TContainer>() first.");

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
                $"FExServiceProvider not initialized. Call {nameof(InitializeAsync)}<TContainer>() first.");

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
                $"FExServiceProvider not initialized. Call {nameof(InitializeAsync)}<TContainer>() first.");

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
                $"FExServiceProvider not initialized. Call {nameof(InitializeAsync)}<TContainer>() first.");

        return await ServiceContainer.ResolveServicesAsync<T>();
    }

    /// <summary>
    /// Retrieves the <see cref="T" /> instance or returns default value.
    /// <br />
    /// <b>⚠️ This is discouraged</b> and should only be used where Dependency Injection is unavailable.
    /// </summary>
    public static T? GetOrDefault<T>() => RequiredServiceContainer.ResolveOrDefault(default(T));

    public static T? GetOrDefault<T>(T? fallback) => RequiredServiceContainer.ResolveOrDefault(fallback);

    /// <summary>
    /// Disposes the container.
    /// </summary>
    public static void Release()
    {
        ServiceContainer?.Release();
        ServiceContainer = null;
        ServiceProvider?.Dispose();
        ServiceProvider = null;
        _containerInstance?.Dispose();
        _containerInstance = null;
    }

    /// <summary>
    /// Default startup point of application. Creates DI container instance and wraps into service,
    /// that allows to distribute its registrations around the app.
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    /// <returns></returns>
    public static ValueTask<TContainer> InitializeAsync<TContainer>() where TContainer : class, IDisposable, new() =>
        InitializeAsync<TContainer>(null, null);

    public static ValueTask<TContainer> InitializeAsync<TContainer>(IServiceCollection? services)
        where TContainer : class, IDisposable, new() =>
        InitializeAsync<TContainer>(services, null);

    public static async ValueTask<TContainer> InitializeAsync<TContainer>(IServiceCollection? services,
                                                                          Action<TContainer>? configureContainer)
        where TContainer : class, IDisposable, new()
    {
        // Idempotent: return existing container if already initialized with same type
        if (_containerInstance is TContainer existingContainer)
            return existingContainer;

        // Thread-safety: prevent concurrent initialization
        await InitializationLock.WaitAsync();

        try
        {
            // Double-check after acquiring lock
            if (_containerInstance is TContainer existing)
                return existing;

            // Dispose previously set multi-di provider if any
            Release();
            var container = new TContainer();
            configureContainer?.Invoke(container);
            _containerInstance = container;

            var serviceProvider = ((IContainer<IFExStrongInjectServiceProvider>)container)
                .Resolve<IFExStrongInjectServiceProvider>()
                .Value;

            serviceProvider.SetServiceProvider(container);
            ServiceProvider = serviceProvider;

            // Set ServiceContainer by resolving from the new container
#pragma warning disable IDISP004 // Don't ignore created IDisposable
            ServiceContainer = container is IContainer<IFExServiceContainer> containerResolver
                ? containerResolver.Resolve<IFExServiceContainer>().Value
                : throw new InvalidOperationException(
                    $"{typeof(TContainer).Name} must implement IContainer<IFExServiceContainer>");
#pragma warning restore IDISP004

            // Register services with the container
            ServiceContainer.RegisterServices(container, services);

            var serviceProviders = (await GetAllAsync<IFExServiceProvider>()).Except([serviceProvider]).ToArray();
            await serviceProviders.WithWhenAllAsync(static sp => sp.ConfigureServiceProviderAsync());

            return container;
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    public static TModule GetDefaultContainer<TModule>() where TModule : class
    {
        try
        {
            // Null ServiceProvider intentionally throws here (NRE), handled below via the explicit null check.
            return ServiceProvider!.GetContainer<TModule>();
        }
        catch (Exception ex)
        {
            object? provider = null;

            try
            {
                provider = ServiceProvider!.GetContainer<object>();
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
        var provider = ServiceProvider.Guard(nameof(ServiceProvider));
        await serviceProviderConfiguration(provider);

        return provider;
    }

    #region IDisposable
    /// <summary>
    /// Disposes the service provider.
    /// </summary>
    public void Dispose() => ServiceContainer?.Dispose();
    #endregion
}