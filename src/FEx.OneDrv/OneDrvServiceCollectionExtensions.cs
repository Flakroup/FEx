using FEx.OneDrv.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.OneDrv;

public static class OneDrvServiceCollectionExtensions
{
    public static IServiceCollection AddOneDrv(this IServiceCollection services, OneDriveOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IOneDriveAuthService, MsalAuthService>();
        services.AddSingleton<IOneDriveItemEnumerator, OneDriveItemEnumerator>();
        services.AddSingleton<IOneDriveThumbnailService, OneDriveThumbnailService>();
        services.AddSingleton<IOneDriveClient, OneDriveClient>();
        return services;
    }
}
