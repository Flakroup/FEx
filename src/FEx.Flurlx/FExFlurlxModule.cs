using FEx.Abstractions.Interfaces;
using FEx.Flurlx.Abstractions.Interfaces;
using Flurl.Http.Configuration;
using StrongInject;

namespace FEx.Flurlx;

[Register(typeof(FlurlConfigurator), Scope.SingleInstance, typeof(IFlurlConfigurator))]
[Register(typeof(FlurlClientCache), Scope.SingleInstance, typeof(IFlurlClientCache))]
[Register(typeof(FExFlurlxModuleInitializer),
    Scope.SingleInstance,
    typeof(FExFlurlxModuleInitializer),
    typeof(IInitializeModule))]
public class FExFlurlxModule
{
}