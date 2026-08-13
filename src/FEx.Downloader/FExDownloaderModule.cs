using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Downloader.Services;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Downloader;

[Register(typeof(DownloadService), Scope.SingleInstance)]
[Register(typeof(FExDownloader), Scope.SingleInstance, typeof(FExDownloader), typeof(IFExInitializable))]
[Register(typeof(FExDownloaderModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExDownloaderModule : InitializeModule<IFExDownloaderModule, IServiceCollection>
{
    protected override void RegisterServices(IFExDownloaderModule? container, IServiceCollection services)
    {
        container = container.Guard(nameof(container));
        services.AddSingletonServiceUsingContainer<DownloadService>(container);
    }
}