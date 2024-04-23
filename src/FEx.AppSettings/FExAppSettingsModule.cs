using FEx.AppSettings.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.AppSettings
{
    [Register(typeof(ConfigurationService), Scope.SingleInstance, typeof(IConfigurationService))]
    [Register(typeof(FExAppSettingsModuleInitializer),
        Scope.SingleInstance,
        typeof(FExAppSettingsModuleInitializer),
        typeof(IInitializeModule))]
    public class FExAppSettingsModule
    {
        public static void AddServices(IFExAppSettingsModule container, IServiceCollection services)
        {
            services.AddSingletonServiceUsingContainer<IConfigurationService>(container);
        }
    }
}
