using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Json.Abstractions;
using FEx.Json.Abstractions.Helpers;
using FEx.Json.SystemTextJsonx.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System;
using System.Text.Json;

namespace FEx.Json.SystemTextJsonx;

/// <summary>
/// Registers <see cref="FExSystemTextJsonSerializer" /> as <see cref="IFExJsonSerializer" />. Register this module or
/// <c>FExJsonModule</c> (Newtonsoft.Json), not both: each provides <see cref="IFExJsonSerializer" /> and <see cref="DIMeta" />.
/// </summary>
[Register(typeof(DIMeta), Scope.SingleInstance, typeof(DIMeta), typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(DIJsonTypeInfoModifier), Scope.SingleInstance)]
[Register(typeof(FExSystemTextJsonSerializer), Scope.SingleInstance, typeof(IFExJsonSerializer))]
[Register(typeof(FExSystemTextJsonModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExSystemTextJsonModule : InitializeModule<IFExSystemTextJsonContainer, IServiceCollection>
{
    /// <summary>
    /// The options the serializer is built from; required, and set before the container resolves the serializer.
    /// Use <c>FExSystemTextJsonOptions.CreateDefault()</c> for the reflection-based defaults, or, in a trimmed or
    /// Native AOT application, <c>FExSystemTextJsonOptions.CreateDefault(MyJsonContext.Default)</c> or any options whose
    /// <see cref="JsonSerializerOptions.TypeInfoResolver" /> is a source-generated <c>JsonSerializerContext</c>. The
    /// module does not pick a reflection default itself, so it stays Native AOT safe. It works on a copy with the DI
    /// modifier added, so these options stay usable on their own.
    /// </summary>
    public static JsonSerializerOptions? Options { get; set; }

    /// <exception cref="InvalidOperationException"><see cref="Options" /> is not set.</exception>
    [Factory(Scope.SingleInstance)]
    public static JsonSerializerOptions JsonSerializerOptionsFactory(DIJsonTypeInfoModifier modifier) =>
        FExSystemTextJsonOptions.WithDIConstruction(
            Options
            ?? throw new InvalidOperationException(
                $"Set {nameof(FExSystemTextJsonModule)}.{nameof(Options)} before resolving the serializer, for example to FExSystemTextJsonOptions.CreateDefault()."),
            modifier);

    protected override void RegisterServices(IFExSystemTextJsonContainer? container, IServiceCollection services)
    {
        if (container is null)
            throw new ArgumentNullException(nameof(container));

        services.AddTransientServiceUsingContainer<DIMeta>(container);
        services.AddSingletonServiceUsingContainer<JsonSerializerOptions>(container);
        services.AddSingletonServiceUsingContainer<IFExJsonSerializer>(container);
    }
}
