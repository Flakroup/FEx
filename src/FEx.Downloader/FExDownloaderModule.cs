using FEx.DI.Abstractions.Interfaces;
using FEx.Downloader.Services;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Downloader;

[Register(typeof(DownloadService), Scope.SingleInstance)]
[Register(typeof(FExDownloader),
    Scope.SingleInstance,
    typeof(FExDownloader),
    typeof(IInitializeModule))]
public class FExDownloaderModule
{
    public static void AddServices(IFExDownloaderModule module, IServiceCollection services) => services.AddSingletonServiceUsingContainer<DownloadService>(module);
}