using FEx.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Avaloniax;

[Register(typeof(AvaloniaDispatcher), typeof(IFExDispatcher))]
[Register(typeof(AvaloniaMessagePopupService), typeof(IMessagePopupService))]
public class FExAvaloniaxModule
{
}