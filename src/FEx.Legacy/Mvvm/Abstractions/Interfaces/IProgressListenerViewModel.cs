using FEx.MVVM.Abstractions.Interfaces;

namespace FEx.Legacy.Mvvm.Abstractions.Interfaces;

public interface IProgressListenerViewModel<out T> : IProgressReceiver<T>, IThreadingAwareViewModel
    where T : IProgressAggregator
{
}