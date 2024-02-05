using FEx.Downloader.Services;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Downloader;

[Register(typeof(DownloadService), Scope.SingleInstance)]
public class FExDownloaderModule
{
    public static void AddServices<TContainer>(IServiceCollection services)
        where TContainer : class, IFExDownloaderModule
    {
        services.AddSingletonServiceUsingContainer<TContainer, DownloadService>();
    }
}