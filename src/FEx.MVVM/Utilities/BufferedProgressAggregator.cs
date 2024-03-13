using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Enums;
using FEx.MVVM.Subjects;
using FEx.Rx.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.MVVM.Utilities;

public class BufferedProgressAggregator : ProgressAggregator
{
    private readonly ProgressChangesBuffer _changesBuffer;
    private readonly ProgressChangeSubject _progressChangeSubject;
    private readonly IDisposable _changeSubscription;
    private readonly SemaphoreSlim _progressLock;
    private readonly SemaphoreSlim _progressQueueLock;
    private bool _isDisposed;

    public static TimeSpan ChangesBufferingDelay { get; set; } = TimeSpan.FromMilliseconds(25);

    public BufferedProgressAggregator()
    {
        _progressChangeSubject = new ProgressChangeSubject();
        _progressQueueLock = new SemaphoreSlim(1, 1);
        _progressLock = new SemaphoreSlim(1, 1);
        _changesBuffer = new ProgressChangesBuffer();

        _changeSubscription = _progressChangeSubject.Timestamp()
            .Buffer(ChangesBufferingDelay)
            .SubscribeTask(ProcessChangesAsync);
    }

    /// <summary>
    /// Sets current progress value and maximal allowed value of the ProgressBar
    /// </summary>
    /// <param name="value">Progress value to be added or set</param>
    /// <param name="maximum">Maximal allowed value.</param>
    /// <param name="mode">Progress change mode. ProgressChangeMode.End sets ProgressValue to current ProgressMaximum.</param>
    public override void PrgSet(double? value, double? maximum = null, ProgressChangeMode mode = ProgressChangeMode.Set)
    {
        if (!Timer.IsRunning
            && (maximum.HasValue || value.HasValue)
            && mode != ProgressChangeMode.End)
            Timer.TimerStart();

        if (maximum.HasValue)
            _progressChangeSubject.OnNext(new ProgressChange<double>
            {
                PropertyName = nameof(IProgressStatus.Maximum),
                Value = maximum.Value,
                ChangeMode = mode
            });

        if (value.HasValue)
            _progressChangeSubject.OnNext(new ProgressChange<double>
            {
                PropertyName = nameof(IProgressStatus.Value),
                Value = value.Value,
                ChangeMode = mode
            });
    }

    protected virtual void ProcessChange(IProgressChange progressChange)
    {
        switch (progressChange)
        {
            case ProgressChange<double> doubleProgressChange:
                ChangeDoubleValues(doubleProgressChange);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(progressChange),
                    $"Type {progressChange.GetType().Name} is not handled");
        }
    }

    protected virtual void ChangeDoubleValues(ProgressChange<double> progressChange)
    {
        switch (progressChange.PropertyName)
        {
            case nameof(IProgressStatus.Value):
            case nameof(IProgressStatus.Maximum):
                ProcessProgressChange(progressChange);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(progressChange.PropertyName),
                    $"{progressChange.PropertyName} is not handled");
        }
    }

    protected virtual void ChangeDoubleValue(ProgressChange<double> progressChange,
                                             Func<double> get,
                                             Action<double> set)
    {
        switch (progressChange.ChangeMode)
        {
            case ProgressChangeMode.Set:
                set(progressChange.Value);

                break;
            case ProgressChangeMode.Add:
                set(get() + progressChange.Value);

                break;
            case ProgressChangeMode.End:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected virtual void ProcessProgressChange(ProgressChange<double> progressChange)
    {
        switch (progressChange.ChangeMode)
        {
            case ProgressChangeMode.Add:
            {
                switch (progressChange.PropertyName)
                {
                    case nameof(IProgressStatus.Value):
                        ProcessAddPrg(progressChange.Value, null);

                        break;
                    case nameof(IProgressStatus.Maximum):
                        ProcessAddPrg(null, progressChange.Value);

                        break;
                }

                break;
            }
            case ProgressChangeMode.Set:
            {
                switch (progressChange.PropertyName)
                {
                    case nameof(IProgressStatus.Value):
                        ProcessSetPrg(progressChange.Value, null);

                        break;
                    case nameof(IProgressStatus.Maximum):
                        ProcessSetPrg(null, progressChange.Value);

                        break;
                }

                break;
            }
            case ProgressChangeMode.End:
                ProcessEndPrg();

                break;
            default:
                throw new ArgumentOutOfRangeException($"{progressChange.ChangeMode} is not handled");
        }
    }

    private async Task ProcessChangesAsync(IList<Timestamped<IProgressChange>> changesBatch,
                                           CancellationToken cancellationToken)
    {
        await _changesBuffer.AddToBufferAsync(changesBatch, cancellationToken);

        if (!await _progressQueueLock.WaitAsync(TimeSpan.Zero, cancellationToken))
            return;

        try
        {
            if (!await _progressLock.WaitAsync(TimeSpan.Zero, cancellationToken))
                return;
        }
        finally
        {
            _progressQueueLock.Release();
        }

        try
        {
            IList<Timestamped<IProgressChange>> changes =
                await _changesBuffer.RetrieveFromBufferAsync(cancellationToken);

            while (changes.Any())
            {
                foreach (Timestamped<IProgressChange> change in changes)
                    ProcessChange(change.Value);

                changes = await _changesBuffer.RetrieveFromBufferAsync(cancellationToken);
            }
        }
        finally
        {
            _progressLock.Release();
        }
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            _changeSubscription?.Dispose();
            _progressLock?.Dispose();
            _progressQueueLock?.Dispose();
            _progressChangeSubject?.Dispose();
            _changesBuffer?.Dispose();
        }

        _isDisposed = true;
        base.Dispose(disposing);
    }
    #endregion
}