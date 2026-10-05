using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StrongInject;

namespace FEx.Json.Tests.Contract;

/// <summary>Backs <c>FExServiceProvider</c> with a transient <see cref="IDIModel" /> that marks its origin.</summary>
[RegisterModule(typeof(FExDependencyInjectionModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance)]
public sealed partial class ServiceProviderTestContainer : IFExDependencyInjectionContainer, IContainer<IFExServiceProvider>,
    IContainer<FExMicrosoftDIServiceProvider>, IContainer<IInitializeModule<IServiceCollection>[]>, IContainer<IDIModel>
{
    public const string Origin = "container";

    [Factory]
    public static ILogger CreateLogger() => NullLogger.Instance;

    // The provider asks for these collections; nothing in this container contributes to them.
    [Factory]
    public static IFExInitializable[] CreateInitializables() => [];

    [Factory]
    public static IMicrosoftDIConfigurator[] CreateMicrosoftDIConfigurators() => [];

    [Factory]
    public static IConfigurator[] CreateConfigurators() => [];

    [Factory]
    public static IAsyncConfigurator[] CreateAsyncConfigurators() => [];

    [Factory]
    public static IDIModel CreateModel() => new DIModel { Origin = Origin };
}
