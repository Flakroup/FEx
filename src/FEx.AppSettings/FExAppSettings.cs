using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.AppSettings;

public class FExAppSettings : InitializeModule<IFExAppSettingsModule>
{
    protected override void AddServices(IFExAppSettingsModule container, IServiceCollection services) =>
        FExAppSettingsModule.AddServices(container, services);
}