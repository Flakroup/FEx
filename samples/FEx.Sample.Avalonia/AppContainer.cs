using FEx.Avaloniax;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Flurlx;
using FEx.Flurlx.Abstractions.Interfaces;
using FEx.Sample.Avalonia.Configuration;
using FEx.Sample.Avalonia.Services;
using StrongInject;

namespace FEx.Sample.Avalonia;

/// <summary>
/// StrongInject container for the Avalonia application.
/// Demonstrates FEx Multi-DI with Avalonia + Flurlx integration.
/// </summary>
[RegisterModule(typeof(FExModule))]
[RegisterModule(typeof(FExFlurlxModule))]
[Register(typeof(JsonPlaceholderApiConfiguration), Scope.SingleInstance, typeof(IApiConfiguration))]
[Register(typeof(JsonPlaceholderApi), Scope.SingleInstance, typeof(JsonPlaceholderApi))]
[RegisterModule(typeof(FExModule))]
public sealed partial class AppContainer : FExModule, IFExContainer, IFExFlurlxContainer, IContainer<JsonPlaceholderApi>
{
    [Instance]
    public static IAsyncConfigurator[] AsyncConfigurators { get; } = [];

    [Instance]
    public static IMicrosoftDIConfigurator[] MicrosoftDIConfigurators { get; } = [];
}