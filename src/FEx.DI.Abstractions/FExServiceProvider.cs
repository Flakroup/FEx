using FEx.Abstractions.Interfaces;
using FEx.Common.Extensions;
using FEx.DI.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FEx.DI.Abstractions;

public static class FExServiceProvider
{
    private static IFExServiceProvider _serviceProvider;

    public static TContainer Initialize<TContainer, TProvider>() where TContainer : class, IDisposable, new()
        where TProvider : class, IFExStrongInjectServiceProvider, new()
    {
        _serviceProvider?.Dispose();
        var serviceProvider = new TProvider();

        TContainer container = serviceProvider.ConfigureServiceProvider<TContainer>();
        _serviceProvider = serviceProvider;
        Initialize();

        return container;
    }

    public static async Task InitializeAsync<TProvider>(Func<IServiceCollection, IServiceCollection> configuration =
                                                            null,
                                                        IServiceCollection services = null)
        where TProvider : class, IFExMicrosoftDIServiceProvider
    {
        _serviceProvider?.Dispose();
        TProvider serviceProvider = _serviceProvider!.GetRequiredService<TProvider>();

        await serviceProvider.ConfigureServiceProviderAsync(configuration, services);
        _serviceProvider = serviceProvider;
        Initialize();
    }

    [Obsolete("Strongly advised against. Use DI instead!")]
    public static T Get<T>() => _serviceProvider.GetRequiredService<T>();

    [Obsolete("Strongly advised against. Use DI instead!")]
    public static object Get(Type serviceType) => _serviceProvider.GetRequiredService(serviceType);

    public static TModule GetDefaultContainer<TModule>() where TModule : class
    {
        try
        {
            return _serviceProvider.GetContainer<TModule>();
        }
        catch (Exception ex)
        {
            object provider = null;

            try
            {
                provider = _serviceProvider.GetContainer<object>();
            }
            catch
            {
                //ignored
            }

            if (provider is not null)
                throw new InvalidOperationException(
                    $"Default container {provider?.GetType().FullName ?? "null"} does not implement interface or type {typeof(TModule).FullName}",
                    ex);

            if (_serviceProvider is null)
                throw new InvalidOperationException("Service provider wasn't initialized");

            throw;
        }
    }

    public static async Task<IFExServiceProvider> RegisterDependenciesAsync(
        Func<IFExMicrosoftDIServiceProvider, Task> serviceProviderConfiguration)
    {
        serviceProviderConfiguration.Guard(nameof(serviceProviderConfiguration));
#pragma warning disable CS0618 // Type or member is obsolete
        IFExMicrosoftDIServiceProvider provider = Get<IFExMicrosoftDIServiceProvider>();
#pragma warning restore CS0618 // Type or member is obsolete
        await serviceProviderConfiguration(provider);

        return provider;
    }

    private static void Initialize()
    {
        IFExInitialize[] initializers = _serviceProvider.GetRequiredService<IFExInitialize[]>();

        foreach (IFExInitialize initializer in initializers)
            initializer.Initialize();
    }
}