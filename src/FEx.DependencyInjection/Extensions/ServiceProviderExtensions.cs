using StrongInject;

namespace FEx.DependencyInjection.Extensions;

public static class ServiceProviderExtensions
{
    public static T GetService<T>(this IContainer<T> serviceProvider)
    {
#pragma warning disable IDISP004
        return serviceProvider.Resolve<T>().Value;
#pragma warning restore IDISP004
    }
}