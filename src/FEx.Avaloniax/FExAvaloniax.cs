using FEx.Agnostics.Abstractions;
using ReactiveUI;

namespace FEx.Avaloniax;

/// <summary>
/// Initializer for FEx.Avaloniax that configures ReactiveUI with Avalonia's UI scheduler.
/// </summary>
public class FExAvaloniax : FExInitializable
{
    protected override void OnInitialize()
    {
        // Configure ReactiveUI to use Avalonia's UI thread scheduler
        RxSchedulers.MainThreadScheduler = AvaloniaScheduler.Instance;
    }
}