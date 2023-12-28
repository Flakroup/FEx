using FEx.Abstractions;
using FEx.Flurlx.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Flurlx;

[Register(typeof(FlurlConfigurator), Scope.SingleInstance, typeof(IFlurlConfigurator))]
[Register(typeof(FExFlurlxModuleInitializer), Scope.SingleInstance, typeof(FExFlurlxModuleInitializer), typeof(IInitializeModule))]
public class FExFlurlxModule
{
    public static void Initialize(IFlurlConfigurator configurator)
    {
        configurator.Configure();
    }
}