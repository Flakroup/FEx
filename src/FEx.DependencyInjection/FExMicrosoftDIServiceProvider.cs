using FEx.Abstractions;
using FEx.Fundamentals;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace FEx.DependencyInjection;

public class FExMicrosoftDIServiceProvider : IFExServiceProvider
{
    private ServiceProvider _provider;

    public T GetRequiredService<T>() => _provider.GetRequiredService<T>();

    public T TryResolveService<T>() => _provider.GetService<T>();

    public T GetRequiredService<T>(Type serviceType) => (T)_provider.GetRequiredService(serviceType);

    public object GetRequiredService(Type serviceType) => _provider.GetRequiredService(serviceType);

    public IServiceScope CreateScope() => _provider.CreateScope();

    public object GetService(Type serviceType) => _provider.GetService(serviceType);

    public void ConfigureServiceProvider(Func<IServiceCollection, IServiceCollection> configuration = null,
                                         IServiceCollection services = null)
    {
        services = ConfigureServices(configuration, services);

        try
        {
            _provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true
            });
        }
        catch (AggregateException ex) when (Debugger.IsAttached)
        {
            var sb = new StringBuilder();

            foreach (string m in ex.InnerExceptions.Select(e => e.Message.Split(':')[4])
                         .Distinct()
                         .OrderBy(x => x)
                         .ToList())
                sb.AppendLine(m);

            Console.WriteLine(sb.ToString()); //todo logger

            throw;
        }
    }

    private IServiceCollection ConfigureServices(Func<IServiceCollection, IServiceCollection> configuration,
                                                 IServiceCollection services)
    {
        services ??= new ServiceCollection();

        if (configuration is not null)
            services = configuration(services);

        services.AddSingleton<IScopeProvider>(this)
            .AddSingleton<IFExServiceProvider>(this)
            .AddSingleton(Foundation.StrongInjectServiceProvider.GetRequiredService<Foundation>())
            .AddSingleton(Foundation.AsyncHelper)
            .AddSingleton(Foundation.StrongInjectServiceProvider.GetRequiredService<ITasksInfoSubject>());

        return services;
    }
}