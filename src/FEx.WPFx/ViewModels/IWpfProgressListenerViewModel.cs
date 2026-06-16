using FEx.Legacy.Mvvm.Abstractions.Interfaces;
using System.Windows.Controls;

namespace FEx.WPFx.ViewModels;

public interface IWpfProgressListenerViewModel<out T> : IWpfProgressReceiver<T>, IThreadingAwareViewModel
    where T : IWpfProgressStatusContainer
{
    ContentControl View { get; set; }
    string ViewName { get; set; }
}