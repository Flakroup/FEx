using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.AppSettings.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.AppSettings;

[Register(typeof(ConfigurationService), Scope.SingleInstance, typeof(IConfigurationService))]
[Register(typeof(FExAppSettings), Scope.SingleInstance, typeof(FExAppSettings), typeof(IFExInitializable))]
[Register(typeof(FExAppSettingsModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExAppSettingsModule : InitializeModule<IFExAppSettingsModule, IServiceCollection>
{
    protected override void RegisterServices(IFExAppSettingsModule? container, IServiceCollection services)
    {
        container = container.Guard(nameof(container));

        services.AddSingletonServiceUsingContainer<IConfigurationService>(container);
    }
}