using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Json.Extensions;
using FEx.Json.Helpers;
using FEx.Json.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System;

namespace FEx.Json;

[Register(typeof(DIMeta), Scope.SingleInstance, typeof(DIMeta), typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(DIContractResolver), Scope.SingleInstance, typeof(IContractResolver))]
[Register(typeof(FExJson), Scope.SingleInstance, typeof(FExJson), typeof(IFExInitializable))]
[Register(typeof(FExJsonModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExJsonModule : InitializeModule<IFExJsonContainer, IServiceCollection>
{
    [Factory]
    // DefaultSettings is backed by JsonExtensions' static ctor and is always non-null here.
    public static JsonSerializerSettings JsonSerializerSettingsFactory() => JsonExtensions.DefaultSettings!;

    protected override void RegisterServices(IFExJsonContainer? container, IServiceCollection services)
    {
        if (container is null)
            throw new ArgumentNullException(nameof(container));

        services.AddTransientServiceUsingContainer<DIMeta>(container);
        services.AddTransientServiceUsingContainer<JsonSerializerSettings>(container);

        services.AddSingletonServiceUsingContainer<IContractResolver>(container);
    }
}