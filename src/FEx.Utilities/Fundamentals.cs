using FEx.Abstractions;
using FEx.Extensions;
using FEx.Utilities.Helpers;
using FEx.Utilities.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FEx.Utilities;

public class Fundamentals
{
    public static IFExDispatcher Dispatcher { get; private set; }
    public static ILogger Logger { get; private set; }
    public static AsyncHelper AsyncHelper { get; private set; }
    public static IFExServiceProvider ServiceProvider { get; private set; }

    public Fundamentals(IFExDispatcher dispatcher, ILogger logger, AsyncHelper asyncHelper)
    {
        Dispatcher = dispatcher;
        Logger = logger;
        AsyncHelper = asyncHelper;
    }

    public static void Init<TContainer>(IFExServiceProvider serviceProvider, Func<IServiceCollection, IServiceCollection> serviceProviderConfiguration = null) where TContainer : class, new()
    {
        serviceProvider.Guard(nameof(serviceProvider));
        serviceProvider.ConfigureServiceProvider<TContainer>(serviceProviderConfiguration);
        ServiceProvider = serviceProvider;
        ServiceProvider.GetRequiredService<Fundamentals>();
    }
}