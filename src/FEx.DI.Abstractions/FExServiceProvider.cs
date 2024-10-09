using FEx.Abstractions.Extensions;
using FEx.Abstractions.Interfaces;
using FEx.Common.Extensions;
using FEx.DI.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.DI.Abstractions;

public static class FExServiceProvider
{
    public static IFExServiceProvider ServiceProvider { get; private set; }

    public static TContainer Initialize<TContainer, TProvider>() where TContainer : class, IDisposable, new()
        where TProvider : class, IFExStrongInjectServiceProvider, new()
    {
        ServiceProvider?.Dispose();
        var serviceProvider = new TProvider();

        TContainer container = serviceProvider.ConfigureServiceProvider<TContainer>();
        ServiceProvider = serviceProvider;
        Initialize(serviceProvider);

        return container;
    }

    public static async Task InitializeAsync<TProvider>(Func<IServiceCollection, IServiceCollection> configuration =
                                                            null,
                                                        IServiceCollection services = null)
        where TProvider : class, IFExMicrosoftDIServiceProvider
    {
        TProvider serviceProvider = ServiceProvider!.GetRequiredService<TProvider>();

        await serviceProvider.ConfigureServiceProviderAsync(configuration, services);
        ServiceProvider = serviceProvider;
        Initialize(serviceProvider);
    }

    [Obsolete("Strongly advised against. Use DI instead!")]
    public static T Get<T>() => ServiceProvider.GetRequiredService<T>();

    [Obsolete("Strongly advised against. Use DI instead!")]
    public static object Get(Type serviceType) => ServiceProvider.GetRequiredService(serviceType);

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
                //ignored
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
        Func<IFExMicrosoftDIServiceProvider, Task> serviceProviderConfiguration)
    {
        serviceProviderConfiguration.Guard(nameof(serviceProviderConfiguration));
#pragma warning disable CS0618 // Type or member is obsolete
        IFExMicrosoftDIServiceProvider provider = Get<IFExMicrosoftDIServiceProvider>();
#pragma warning restore CS0618 // Type or member is obsolete
        await serviceProviderConfiguration(provider);

        return provider;
    }

    private static void Initialize(IFExServiceProvider serviceProvider)
    {
        //todo move that to constructor/use SI interfaces to initailize
        var priorityInitializers = serviceProvider.GetRequiredService<IFExPriorityInitialize[]>()
            .OrderBy(initializer => initializer.Priority)
            .ToList();

        priorityInitializers.InitializeAll();

        IFExInitialize[] initializers = serviceProvider.GetRequiredService<IFExInitialize[]>();
        initializers.InitializeAll();

        IInitializeModule[] modules = serviceProvider.GetRequiredService<IInitializeModule[]>();
        modules.InitializeAll();
    }
}