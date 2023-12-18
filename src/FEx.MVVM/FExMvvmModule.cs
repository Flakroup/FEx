using FEx.Abstractions;
using StrongInject;

namespace FEx.MVVM;

[Register(typeof(AsyncEventDeliverer), Scope.SingleInstance, typeof(IEventDeliverer))]//todo: Move outside
public class FExMvvmModule
{
}