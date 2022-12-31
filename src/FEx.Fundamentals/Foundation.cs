using FEx.Abstractions;
using StrongInject;

namespace FEx.Fundamentals;

public static class Foundation
{
    public static IFExDispatcher Dispatcher { get; private set; }
    public static AsyncHelper AsyncHelper { get; private set; }

    public static void Init<TContainer>(IFExServiceProvider serviceProvider) where TContainer : class, IContainer<IFExDispatcher>, IContainer<AsyncHelper>, new()
    {
        serviceProvider.ConfigureServiceProvider<TContainer>();
        Dispatcher = serviceProvider.GetRequiredService<IFExDispatcher>();
        AsyncHelper = serviceProvider.GetRequiredService<AsyncHelper>();
    }
}