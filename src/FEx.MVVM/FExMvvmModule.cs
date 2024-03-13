using FEx.Abstractions.Interfaces;
using StrongInject;

namespace FEx.MVVM;

[Register(typeof(FExMvvmModuleInitializer),
    Scope.SingleInstance,
    typeof(FExMvvmModuleInitializer),
    typeof(IInitializeModule))]
public class FExMvvmModule
{
}