using FEx.Agnostics.TestMocks;
using FEx.Core;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StrongInject;
using StrongInject.Modules;

namespace FEx.Logging.Tests;

// SI1105: StrongInject emits a benign resolution warning for this test container's module graph;
// the container is test-only and resolves correctly at runtime.
#pragma warning disable SI1105
[RegisterModule(typeof(CollectionsModule))]
[RegisterModule(typeof(FExDependencyInjectionModule))]
[RegisterModule(typeof(FExLoggingModule))]
[RegisterModule(typeof(FExCoreModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance)]
public sealed partial class TestContainer : TestBase, IFExLoggingContainer, IFExDependencyInjectionContainer,
    IFExCoreContainer, IContainer<IFExServiceContainer>, IContainer<IFExServiceProvider>,
    IContainer<FExMicrosoftDIServiceProvider>
{
    [Factory]
    public static ILogger CreateLogger() => NullLogger.Instance;

    [Factory]
    public static ISentryConfig CreateSentryConfig() => Substitute.For<ISentryConfig>();
}