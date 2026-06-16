using FEx.MVVM.Interfaces;

namespace FEx.WPFx.ViewModels;

public interface IWpfProgressReceiver<out T> : IProgressReceiver<T> where T : IWpfProgressStatusContainer
{
}