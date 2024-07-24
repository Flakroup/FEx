using FEx.DI.Abstractions;
using FEx.DI.Abstractions.Interfaces;
using FEx.Extensions;
using FEx.Extensions.Collections.Lists;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEx.DependencyInjection;

public sealed class FExMicrosoftDIServiceProvider : IFExMicrosoftDIServiceProvider
{
    private readonly ILogger _logger;
    private ServiceProvider _provider;

    public FExMicrosoftDIServiceProvider(ILogger logger)
    {
        _logger = logger;
    }

    public async Task ConfigureServiceProviderAsync(Func<IServiceCollection, IServiceCollection> configuration = null,
                                                    IServiceCollection services = null)
    {
        services = await ConfigureServicesAsync(configuration, services);

        try
        {
            if (_provider is not null)
                await _provider.DisposeAsync();

            _provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true
            });
        }
        catch (AggregateException ex)
        {
            var sb = new StringBuilder();

            foreach (string m in ex.InnerExceptions.Select(e => e.Message.Split(':')[4])
                         .Distinct()
                         .OrderBy(x => x)
                         .ToList())
                sb.AppendLine(m);

            _logger.LogError(sb.ToString());

            throw;
        }
    }

    /// <summary>
    ///     Get service of type <typeparamref name="T" /> from the <see cref="IServiceProvider" />.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="T" />.</returns>
    /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T" />.</exception>
    public T GetRequiredService<T>() => _provider.GetRequiredService<T>();

    public T TryResolveService<T>() => _provider.GetService<T>();

    /// <summary>
    ///     Get service of type <paramref name="serviceType" /> from the <see cref="IServiceProvider" />.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serviceType">An object that specifies the type of service object to get.</param>
    /// <returns>
    ///     A service object of type <paramref name="serviceType" />.
    /// </returns>
    /// <exception cref="System.InvalidOperationException">There is no service of type <paramref name="serviceType" />.</exception>
    public T GetRequiredService<T>(Type serviceType) => (T)_provider.GetRequiredService(serviceType);

    public object GetRequiredService(Type serviceType) => _provider.GetRequiredService(serviceType);

    public TContainer GetContainer<TContainer>() where TContainer : class => _provider as TContainer;

    public IServiceScope CreateScope() => _provider.CreateScope();

    public object GetService(Type serviceType) => _provider.GetService(serviceType);

    private static async Task<IServiceCollection> ConfigureServicesAsync(
        Func<IServiceCollection, IServiceCollection> configuration,
        IServiceCollection services)
    {
        configuration ??= x => x;
        services ??= new ServiceCollection();

        await InitializeModulesAsync(services);

        return configuration(services);
    }

    private static async Task InitializeModulesAsync(IServiceCollection services)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        IInitializeModule[] modules = FExServiceProvider.Get<IInitializeModule[]>();
#pragma warning restore CS0618 // Type or member is obsolete

        if (modules.IsNullOrEmptyList())
            return;

        foreach (IInitializeModule initializer in modules)
            initializer.ConfigureServices(services);

        await modules.Where(x => !x.HasBeenCompleted).RunWithWhenAllAsync(m => m.CompleteInitializationAsync(services));
    }

    #region IDisposable
    public async ValueTask DisposeAsync()
    {
        if (_provider != null)
            await _provider.DisposeAsync();
    }

    public void Dispose() => _provider?.Dispose();
    #endregion
}