using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Json.Abstractions.Interfaces;
using FEx.Json.Extensions;
using FEx.Json.Helpers;
using FEx.Json.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Json;

[Register(typeof(DIMetaDefault), typeof(IDIMeta))]
[Register(typeof(DIContractResolver), Scope.SingleInstance, typeof(IContractResolver))]
[Register(typeof(FExJsonModuleInitializer),
    Scope.SingleInstance,
    typeof(FExJsonModuleInitializer),
    typeof(IInitializeModule))]
public class FExJsonModule
{
    [Factory]
    public static JsonSerializerSettings JsonSerializerSettingsFactory() =>
        JsonExtensions.DefaultSettings;

    public static void AddServices(IFExJsonModule container, IServiceCollection services)
    {
        services.AddTransientServiceUsingContainer<IDIMeta>(container);
        services.AddTransientServiceUsingContainer<JsonSerializerSettings>(container);

        services.AddSingletonServiceUsingContainer<IContractResolver>(container);
    }
}