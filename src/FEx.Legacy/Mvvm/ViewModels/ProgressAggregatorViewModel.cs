using FEx.Core.Abstractions.Interfaces;
using FEx.MVVM.Utilities;

namespace FEx.Legacy.Mvvm.ViewModels;

public class ProgressAggregatorViewModel : ProgressListenerViewModel<ProgressAggregator>
{
    public ProgressAggregatorViewModel(params IAsyncInitializable[] dependencies)
        : this(false, dependencies)
    {
    }

    public ProgressAggregatorViewModel(bool useMainProgressContainer, params IAsyncInitializable[] dependencies)
        : base(useMainProgressContainer, dependencies)
    {
    }
}