using FEx.DI.Abstractions.Interfaces;
using FEx.Json.Extensions;
using FEx.Json.Helpers;
using FEx.Json.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Json;

[Register(typeof(DIMeta), Scope.SingleInstance, typeof(DIMeta), typeof(IInitializeModule))]
[Register(typeof(DIContractResolver), Scope.SingleInstance, typeof(IContractResolver))]
[Register(typeof(FExJson), Scope.SingleInstance, typeof(FExJson), typeof(IInitializeModule))]
public class FExJsonModule
{
    [Factory]
    public static JsonSerializerSettings JsonSerializerSettingsFactory() => JsonExtensions.DefaultSettings;

    public static void AddServices(IFExJsonContainer container, IServiceCollection services)
    {
        services.AddTransientServiceUsingContainer<DIMeta>(container);
        services.AddTransientServiceUsingContainer<JsonSerializerSettings>(container);

        services.AddSingletonServiceUsingContainer<IContractResolver>(container);
    }
}