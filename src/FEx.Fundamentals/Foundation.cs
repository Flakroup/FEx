using FEx.Abstractions.Interfaces;
using FEx.DependencyInjection;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Extensions;
using System;
using System.Threading.Tasks;

namespace FEx.Fundamentals;

public class Foundation
{
    private static IFExDispatcher _dispatcher;
    private static IFExServiceProvider _serviceProvider;
    private static FExStrongInjectServiceProvider _strongInjectServiceProvider;

    public static IFExDispatcher Dispatcher
    {
        get => _dispatcher;
        private set => _dispatcher = value.Guard();
    }

    public static IFExServiceProvider ServiceProvider
    {
        get => _serviceProvider.Guard();
        private set => _serviceProvider = value;
    }

    public static FExStrongInjectServiceProvider StrongInjectServiceProvider
    {
        get => _strongInjectServiceProvider.Guard();
        private set => _strongInjectServiceProvider = value;
    }

    public Foundation(IFExDispatcher dispatcher)
    {
        Dispatcher = dispatcher;
    }

    public static TContainer Init<TContainer>(IFExServiceProvider microsoftDiServiceProvider = null)
        where TContainer : class, IDisposable, new()
    {
        _strongInjectServiceProvider?.Dispose();
        StrongInjectServiceProvider = new FExStrongInjectServiceProvider();
        TContainer container = StrongInjectServiceProvider.ConfigureServiceProvider<TContainer>();
        ServiceProvider = microsoftDiServiceProvider ?? StrongInjectServiceProvider;

        return container;
    }

    public static async Task<IFExServiceProvider> RegisterDependenciesAsync(IFExDependencyInjectionModule container, Func<FExMicrosoftDIServiceProvider, Task> serviceProviderConfiguration)
    {
        serviceProviderConfiguration.Guard(nameof(serviceProviderConfiguration));
        FExMicrosoftDIServiceProvider provider = container.Resolve<FExMicrosoftDIServiceProvider>().Value;
        await serviceProviderConfiguration(provider);
        ServiceProvider = provider;

        return ServiceProvider;
    }
}