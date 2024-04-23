using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Downloader;

public class FExDownloaderModuleInitializer : InitializeModule<IFExDownloaderModule>
{
    protected override void OnInitialize()
    {
    }

    protected override void AddServices(IFExDownloaderModule container, IServiceCollection services)
    {
        FExDownloaderModule.AddServices(container, services);
    }
}