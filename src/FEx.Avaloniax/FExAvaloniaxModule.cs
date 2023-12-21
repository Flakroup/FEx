using FEx.Abstractions;
using FEx.MVVM.Abstractions;
using StrongInject;

namespace FEx.Avaloniax;

[Register(typeof(AvaloniaDispatcher), typeof(IFExDispatcher), typeof(IUIContextExecutor))]
public class FExAvaloniaxModule
{
}