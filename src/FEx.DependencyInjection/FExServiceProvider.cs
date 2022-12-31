using FEx.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using System;
using System.Threading.Tasks;

namespace FEx.DependencyInjection;

public class FExServiceProvider : IFExServiceProvider
{
    private static readonly object SyncRoot = new();

    private static FExServiceProvider _instance;

    private object _provider;

    /// <summary>
    ///     Returns a singleton instance of the default IoC Provider. If possible use dependency injection instead.
    /// </summary>
    public static IFExServiceProvider Instance
    {
        get
        {
            if (_instance == null)
                lock (SyncRoot)
                    _instance ??= new();

            return _instance;
        }
    }

    private FExServiceProvider()
    {
    }

    public void ConfigureServiceProvider<TContainer>(Func<IServiceCollection, IServiceCollection> configuration = null) where TContainer : class, new()
    {
        _provider = new TContainer();
    }

    public T GetRequiredService<T>()
    {
        if (_provider is not IContainer<T> container)
            throw new InvalidOperationException($"Couldn't resolve type: {typeof(T).FullName}");

        return container.Resolve<T>()
            .Value;
    }

    public async Task<T> GetRequiredServiceAsync<T>()
    {
        return _provider is IAsyncContainer<T> container
            ? (await container.ResolveAsync<T>()).Value
            : GetRequiredService<T>();
    }

    public T TryResolveService<T>()
    {
        return _provider is IContainer<T> container
            ? container.Resolve<T>()
                .Value
            : default;
    }
}