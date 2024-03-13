using FEx.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.WPFx.Implementations;
using StrongInject;

namespace FEx.WPFx;

[Register(typeof(DispatcherContextExecutor), typeof(IFExDispatcher))]
[Register(typeof(WpfMessagePopupService), typeof(IMessagePopupService))]
public class FExWPFxModule
{
}