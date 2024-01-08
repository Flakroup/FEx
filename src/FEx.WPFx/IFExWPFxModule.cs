using FEx.Abstractions;
using FEx.MVVM.Abstractions.Interfaces;
using StrongInject;

namespace FEx.WPFx;

public interface IFExWPFxModule : IContainer<IFExDispatcher>, IContainer<IUIContextExecutor>
{
}