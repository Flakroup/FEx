using FEx.Agnostics.TestMocks;
using FEx.DependencyInjection;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using StrongInject;
using StrongInject.Modules;

namespace FEx.Sample.WebAPI;

/// <summary>
/// StrongInject container for the Web API application.
/// Demonstrates Multi-DI pattern: StrongInject + Microsoft DI integration.
/// </summary>
[RegisterModule(typeof(CollectionsModule))]
[RegisterModule(typeof(FExDependencyInjectionModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance)]
[Register(typeof(SampleApiModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
#pragma warning disable IDISP025 // StrongInject generated container
public partial class AppContainer : TestBase, IFExDependencyInjectionContainer, IContainer<IFExServiceProvider>,
    IContainer<FExMicrosoftDIServiceProvider>, IContainer<IInitializeModule<IServiceCollection>[]>
#pragma warning restore IDISP025
{
    [Factory]
    public static ILogger CreateLogger() => NullLogger.Instance;
}

/// <summary>
/// Sample API module demonstrating custom module registration with Microsoft DI
/// </summary>
public class SampleApiModule : InitializeOnlyModule
{
    public override ValueTask OnCompleteInitializationAsync(IServiceCollection services) =>
        // Custom API services can be registered here
        // This demonstrates how app-specific services integrate with FEx modules
        ValueTask.CompletedTask;
}