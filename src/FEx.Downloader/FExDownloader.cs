using FEx.DI.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Downloader;

public class FExDownloader : InitializeModule<IFExDownloaderModule>
{
    protected override void AddServices(IFExDownloaderModule container, IServiceCollection services) =>
        FExDownloaderModule.AddServices(container, services);
}