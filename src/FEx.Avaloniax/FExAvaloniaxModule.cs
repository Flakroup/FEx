using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Avaloniax.Services;
using FEx.Core.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Avaloniax;

[Register(typeof(FExAvaloniax), Scope.SingleInstance, typeof(FExAvaloniax), typeof(IFExInitializable))]
[Register(typeof(AvaloniaDispatcher), typeof(IFExDispatcher))]
[Register(typeof(AvaloniaMessagePopupService), typeof(IMessagePopupService))]
[Register(typeof(NavigationService), Scope.SingleInstance, typeof(INavigationService))]
public class FExAvaloniaxModule
{
}