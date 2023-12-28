using System;

namespace FEx.Abstractions;

public interface IFExServiceProvider : IServiceProvider, IScopeProvider, IDisposable
{
    /// <summary>
    ///     Get service of type <typeparamref name="T" /> from the <see cref="IServiceProvider" />.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="T" />.</returns>
    /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T" />.</exception>
    T GetRequiredService<T>();

    T TryResolveService<T>();

    T GetRequiredService<T>(Type serviceType);

    object GetRequiredService(Type serviceType);
}