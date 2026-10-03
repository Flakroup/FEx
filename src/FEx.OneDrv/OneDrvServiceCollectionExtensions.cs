using FEx.OneDrv.Abstractions;
using FEx.OneDrv.Auth;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FEx.OneDrv;

/// <summary>DI registration for the OneDrive module.</summary>
public static class OneDrvServiceCollectionExtensions
{
    /// <summary>Validates <paramref name="options"/> and registers all OneDrive services as singletons.</summary>
    public static IServiceCollection AddOneDrv(this IServiceCollection services, OneDriveOptions options)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<IOneDriveAuthService, MsalAuthService>();
        services.AddSingleton<IGraphServiceClientCache, GraphServiceClientCache>();
        services.AddSingleton<IOneDriveItemEnumerator, OneDriveItemEnumerator>();
        services.AddSingleton<IOneDriveThumbnailService, OneDriveThumbnailService>();
        services.AddSingleton<IOneDriveClient, OneDriveClient>();

        return services;
    }
}