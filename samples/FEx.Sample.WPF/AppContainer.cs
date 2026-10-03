using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Samples.Shared;
using FEx.WPFx;
using FEx.WPFx.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using Scope = StrongInject.Scope;

namespace FEx.Sample.WPF;

/// <summary>
/// StrongInject container for the WPF application.
/// Demonstrates StrongInject-only Multi-DI pattern (no Microsoft DI).
/// </summary>
[RegisterModule(typeof(FExModule))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(DemoDbStartupModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(DemoDataStore), Scope.SingleInstance)]
#pragma warning disable IDISP025, SI1105 // IDISP025: StrongInject generated container; SI1105: benign module-resolution warning
public partial class AppContainer : FExModule, IFExContainer, IContainer<DemoDataStore>,
    IContainer<IInitializeModule<IServiceCollection>[]>
#pragma warning restore IDISP025, SI1105
{
    [Factory(Scope.SingleInstance)]
    public static DemoDbContext CreateDemoDbContext(IAppInfoProvider appInfoProvider) =>
        DemoDbContext.Create(DemoDatabase.GetDatabaseFile(appInfoProvider.AppData));
}