using FEx.Agnostics.TestMocks;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StrongInject;
using StrongInject.Modules;

namespace FEx.DependencyInjection.Tests;

[RegisterModule(typeof(CollectionsModule))]
[RegisterModule(typeof(FExDependencyInjectionModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance)]
public sealed partial class TestContainer : TestBase, IFExDependencyInjectionContainer, IContainer<IFExServiceProvider>,
    IContainer<FExMicrosoftDIServiceProvider>, IContainer<IInitializeModule<IServiceCollection>[]>
{
    [Factory]
    public static ILogger CreateLogger() => NullLogger.Instance;
}