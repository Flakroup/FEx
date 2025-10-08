using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IFExTimer : IFExNotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// The timer interval
    /// </summary>
    TimeSpan Interval { get; }

    /// <summary>
    /// Indicates whether this timer is running.
    /// </summary>
    bool IsRunning { get; }

    IFExTimer WithCallback(Action callback);
    IFExTimer WithAsyncCallback(Func<Task> asyncCallback, CancellationToken cancellationToken = default);
    IFExTimer WithInterval(double milliseconds);
    IFExTimer WithInterval(TimeSpan interval);

    /// <summary>
    /// Starts the timer.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the timer.
    /// </summary>
    void Stop();
}