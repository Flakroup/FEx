using FEx.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using ReactiveUI;

namespace FEx.Legacy.Mvvm.Abstractions.Interfaces;

public interface IThreadingAwareViewModel : IRunAsync, IReactiveNotifyPropertyChanged<IReactiveObject>,
    IHandleObservableErrors, IViewModelBase, IAsyncInitializable, ILinkableNotifyPropertyChanged
{
    /// <summary>
    ///     Gets or sets a value indicating whether View instance related with this ViewModel is unlocked.
    /// </summary>
    /// <value>
    ///     <c>true</c> if related instance of View is unlocked; otherwise, <c>false</c>.
    /// </value>
    bool IsUiUnlocked { get; set; }

    void PostMainJob(bool showTimeInfo = true);
    void PreMainJob();
}