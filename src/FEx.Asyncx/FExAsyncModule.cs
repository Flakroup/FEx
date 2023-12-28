using FEx.Abstractions;
using StrongInject;

namespace FEx.Asyncx;

[Register(typeof(FExAsyncModuleInitializer), Scope.SingleInstance, typeof(FExAsyncModuleInitializer), typeof(IInitializeModule))]
public class FExAsyncModule
{
}