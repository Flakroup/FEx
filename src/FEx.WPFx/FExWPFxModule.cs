using FEx.Abstractions;
using FEx.MVVM.Abstractions.Interfaces;
using StrongInject;

namespace FEx.WPFx;

[Register(typeof(DispatcherContextExecutor), typeof(IUIContextExecutor), typeof(IFExDispatcher))]
public class FExWPFxModule
{
}