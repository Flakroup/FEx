using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEx.DependencyInjection;

public sealed class FExMicrosoftDIServiceProvider : IFExServiceProvider
{
    private readonly ILogger _logger;
    private ServiceProvider _provider;

    public FExMicrosoftDIServiceProvider(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Configure Microsoft DI service provider by discovering and running Microsoft DI specific modules.
    /// </summary>
    public async ValueTask ConfigureServiceProviderAsync()
    {
        var services = new ServiceCollection();

        await InitializeMicrosoftDIModulesAsync(services);

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

            foreach (var m in ex.InnerExceptions.Select(e => e.Message.Split(':')[4])
                         .Distinct()
                         .OrderBy(x => x)
                         .ToList())
                sb.AppendLine(m);

            _logger.LogError(sb.ToString());

            throw;
        }
    }

    /// <summary>
    /// Get service of type <typeparamref name="T" /> from the <see cref="IServiceProvider" />.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="T" />.</returns>
    /// <exception cref="InvalidOperationException">There is no service of type <typeparamref name="T" />.</exception>
    public T GetRequiredService<T>() => _provider.GetRequiredService<T>();

    public T TryResolveService<T>() => _provider.GetService<T>();

    /// <summary>
    /// Get service of type <paramref name="serviceType" /> from the <see cref="IServiceProvider" />.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serviceType">An object that specifies the type of service object to get.</param>
    /// <returns>
    /// A service object of type <paramref name="serviceType" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">There is no service of type <paramref name="serviceType" />.</exception>
    public T GetRequiredService<T>(Type serviceType) => (T)_provider.GetRequiredService(serviceType);

    public object GetRequiredService(Type serviceType) => _provider.GetRequiredService(serviceType);

    public TContainer GetContainer<TContainer>() where TContainer : class => _provider as TContainer;

    public IScopeProvider CreateScope() => new MicrosoftDIScopeProviderAdapter(_provider);

    public T GetInstance<T>() => GetRequiredService<T>();

    public object GetInstance(Type serviceType) => GetRequiredService(serviceType);

    public object GetService(Type serviceType) => _provider.GetService(serviceType);

    private static async Task InitializeMicrosoftDIModulesAsync(IServiceCollection services)
    {
        // Use TryResolveServices to gracefully handle cases where no modules are registered
        var modules = FExServiceProvider.ServiceContainer.TryResolveServices<IInitializeModule<IServiceCollection>>()
            .ToArray();

        if (modules.IsNullOrEmptyList())
            return;

        foreach (var module in modules)
            module.RegisterServices(services);

        await modules.Where(static x => !x.HasBeenCompleted)
            .WithWhenAllAsync(m => m.CompleteInitializationAsync(services));
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

internal sealed class MicrosoftDIScopeProviderAdapter : IScopeProvider
{
    private readonly IServiceProvider _serviceProvider;

    public MicrosoftDIScopeProviderAdapter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IServiceScope CreateScope() => _serviceProvider.CreateScope();
}