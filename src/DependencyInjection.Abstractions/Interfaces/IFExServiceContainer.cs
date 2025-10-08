using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IFExServiceContainer : IDisposable
{
    void RegisterServices<TContainer>(TContainer container, IServiceCollection services = null)
        where TContainer : class, IDisposable;

    T ResolveService<T>();
    Task<T> ResolveServiceAsync<T>();
    Task<IEnumerable<T>> ResolveServicesAsync<T>();
    IEnumerable<T> ResolveServices<T>();
    IEnumerable<T> TryResolveServices<T>();
    T ResolveOrDefault<T>(T fallback = default);
    void Release();
}