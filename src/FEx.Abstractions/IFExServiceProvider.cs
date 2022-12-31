using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FEx.Abstractions;

public interface IFExServiceProvider
{
    void ConfigureServiceProvider<TContainer>(Func<IServiceCollection, IServiceCollection> configuration = null) where TContainer : class, new();

    /// <summary>
    ///     Get service of type <typeparamref name="T" /> from the <see cref="IServiceProvider" />.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="T" />.</returns>
    /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T" />.</exception>
    T GetRequiredService<T>();

    Task<T> GetRequiredServiceAsync<T>();
    T TryResolveService<T>();
}