using FEx.Abstractions;
using FEx.MVVM.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Avaloniax;

[Register(typeof(AvaloniaDispatcher), typeof(IFExDispatcher), typeof(IUIContextExecutor))]
public class FExAvaloniaxModule
{
}