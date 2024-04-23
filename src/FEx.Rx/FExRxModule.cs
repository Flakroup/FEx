using FEx.DependencyInjection.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Rx;

[Register(typeof(FExRxModuleInitializer),
    Scope.SingleInstance,
    typeof(FExRxModuleInitializer),
    typeof(IInitializeModule))]
public class FExRxModule
{
}