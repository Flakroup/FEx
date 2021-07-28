using Microsoft.Extensions.DependencyInjection;
using System;

namespace FEx.Abstractions
{
    public interface IFExIoCProvider
    {
        void BuildServiceProvider();
        IServiceProvider ConfigureServiceProvider(Func<IServiceCollection, IServiceCollection> configuration = null, IServiceCollection services = null, bool buildProvider = true);
        object GetRequiredService(Type serviceType);
        T GetRequiredService<T>();
        T GetRequiredService<T>(Type serviceType);
        T GetService<T>();
    }
}