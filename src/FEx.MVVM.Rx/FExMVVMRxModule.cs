using FEx.DI.Abstractions.Interfaces;
using StrongInject;

namespace FEx.MVVM.Rx;

[Register(typeof(FExMVVMRx), Scope.SingleInstance, typeof(FExMVVMRx), typeof(IInitializeModule))]
public class FExMVVMRxModule
{
}