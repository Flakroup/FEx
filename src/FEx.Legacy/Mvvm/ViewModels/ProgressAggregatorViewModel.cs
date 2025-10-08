using FEx.Core.Abstractions.Interfaces;
using FEx.MVVM.Utilities;

namespace FEx.Legacy.Mvvm.ViewModels;

public class ProgressAggregatorViewModel : ProgressListenerViewModel<ProgressAggregator>
{
    public ProgressAggregatorViewModel(bool useMainProgressContainer = false, params IAsyncInitializable[] dependencies)
        : base(useMainProgressContainer, dependencies)
    {
    }
}