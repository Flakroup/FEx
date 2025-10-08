using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Avaloniax.Services;
using FEx.Core.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Avaloniax;

[Register(typeof(AvaloniaDispatcher), typeof(IFExDispatcher))]
[Register(typeof(AvaloniaMessagePopupService), typeof(IMessagePopupService))]
[Register(typeof(NavigationService), Scope.SingleInstance, typeof(INavigationService))]
[Register(typeof(AvaloniaExceptionHandler), Scope.SingleInstance, typeof(IExceptionHandler))]
public class FExAvaloniaxModule
{
}