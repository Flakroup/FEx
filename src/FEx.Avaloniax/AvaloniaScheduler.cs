using Avalonia.Threading;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace FEx.Avaloniax;

/// <summary>
/// Scheduler for ReactiveUI that marshals work to Avalonia's UI thread.
/// </summary>
public sealed class AvaloniaScheduler : LocalScheduler
{
    /// <summary>
    /// Gets the singleton instance of the Avalonia scheduler.
    /// </summary>
    public static AvaloniaScheduler Instance { get; } = new();

    private AvaloniaScheduler()
    {
    }

    /// <inheritdoc />
    public override IDisposable Schedule<TState>(TState state,
                                                 TimeSpan dueTime,
                                                 Func<IScheduler, TState, IDisposable> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var composite = new CompositeDisposable(2);
        var cancellation = new CancellationDisposable();

        composite.Add(cancellation);

        if (dueTime == TimeSpan.Zero)
            // Execute immediately on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                if (!cancellation.Token.IsCancellationRequested)
                    composite.Add(action(this, state));
            });
        else
            // Execute after delay on UI thread
#pragma warning disable IDISP004 // Avalonia manages DispatcherTimer lifecycle
            DispatcherTimer.RunOnce(() =>
                {
                    if (!cancellation.Token.IsCancellationRequested)
                        composite.Add(action(this, state));
                },
                dueTime);
#pragma warning restore IDISP004

        return composite;
    }
}