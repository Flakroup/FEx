using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.AppSettings;

public class FExAppSettingsModuleInitializer : InitializeModule<IFExAppSettingsModule>
{
    protected override void OnInitialize()
    {
    }

    protected override void AddServices(IFExAppSettingsModule container, IServiceCollection services)
    {
        FExAppSettingsModule.AddServices(container, services);
    }
}