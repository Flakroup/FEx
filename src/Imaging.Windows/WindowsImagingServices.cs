using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Imaging.Windows;

public class WindowsImagingServices : InitializeModule<IWindowsImagingServicesModule, IServiceCollection>
{
    protected override void RegisterServices(IWindowsImagingServicesModule container, IServiceCollection services)
    {
        WindowsImagingServicesModule.AddServices(container, services);
    }
}
