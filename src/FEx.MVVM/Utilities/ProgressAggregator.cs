using FEx.Basics;
using FEx.Extensions.Base.Converters;
using FEx.Extensions.Base.Enums;
using FEx.Extensions.DateTimes;
using FEx.Extensions.Numericals;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Enums;
using FEx.MVVM.Subjects;
using FEx.Rx;
using FEx.Rx.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.MVVM.Utilities;

public class ProgressAggregator : ProgressStatus, IDisposable
{
    protected readonly FExSubject<string> _changedPropertiesSubject;
    private readonly CompositeDisposable _subscriptions;
    private readonly IDisposable _changeSubscription;
    private readonly SemaphoreSlim _progressLock;
    private readonly SemaphoreSlim _queueLock;
    private readonly List<Timestamped<IProgressChange>> _changesQueue;
    private readonly SemaphoreSlim _progressQueueLock;
    private readonly ProgressChangeSubject _progressChangeSubject;
    private readonly Stopwatch _stopwatch;
    private bool _isDisposed;

    public static TimeSpan ChangesBufferingDelay { get; set; } = TimeSpan.FromMilliseconds(25);

    public string Id { get; }

    private static ProgressChange<double> IncrementChange { get; } = new()
    {
        PropertyName = nameof(IProgressAggregatorProperties.Value),
        Value = 1,
        ChangeMode = ProgressChangeMode.Add
    };

    public ProgressAggregator()
    {
        Id = Guid.NewGuid().ToString();
        _progressChangeSubject = new ProgressChangeSubject();
        _changesQueue = [];
        _queueLock = new SemaphoreSlim(1, 1);
        _progressQueueLock = new SemaphoreSlim(1, 1);
        _progressLock = new SemaphoreSlim(1, 1);
        _stopwatch = new Stopwatch();

        _changedPropertiesSubject = new FExSubject<string>();
        _subscriptions = [];

        //todo if needed Dispose and renew sub on progress Start/End
        _changedPropertiesSubject.Where(ExcludedProperties.Contains)
            .Buffer(FExMvvmConfiguration.DefaultUIRefreshInterval)
            .Distinct()
            .AsyncSubscribe(_subscriptions, propertyNames => OnPropertiesChanged([.. propertyNames]));

        _changeSubscription = _progressChangeSubject.Timestamp()
            .Buffer(ChangesBufferingDelay)
            .SubscribeTask(ProcessChangesAsync);
    }

    public void IncrementProgressValue()
    {
        _progressChangeSubject.OnNext(IncrementChange);
    }

    /// <summary>
    /// Sets current progress value and maximal allowed value of the ProgressBar
    /// </summary>
    /// <param name="val">Progress value to be added or set. -1 sets ProgressValue to current ProgressMaximum.</param>
    /// <param name="max">Maximal allowed value.</param>
    /// <param name="mode">Setting mode. Only Add or Set will do actual work.</param>
    public void PrgSet(double? val, double? max = null, ProgressChangeMode mode = ProgressChangeMode.Set)
    {
        //todo start/stop stopwatch after
        if (max.HasValue)
            _progressChangeSubject.OnNext(new ProgressChange<double>
            {
                Value = max.Value,
                ChangeMode = mode
            });

        if (val.HasValue)
            _progressChangeSubject.OnNext(new ProgressChange<double>
            {
                Value = val.Value,
                ChangeMode = mode
            });
    }

    public void SetProgressIsIndeterminate(bool value)
    {
        IsIndeterminate = value;
        RefreshIsPrgInfoVisible();
    }

    public void SetProgressValue(double value)
    {
        Value = value;

        if (IsIndeterminate && value > 0)
            IsIndeterminate = false;

        RefreshIsPrgInfoVisible();
    }

    public void SetProgressMaximum(double value)
    {
        Maximum = value;
        RefreshIsPrgInfoVisible();
    }

    public void SetPrgInfo(string value)
    {
        Info = value;
    }

    public void SetProgressPercentage(double value)
    {
        Percentage = value;
    }

    public void SetProgressUnit(string value)
    {
        Unit = value;
    }

    public void SetIsPrgInfoVisible(bool value)
    {
        IsInfoVisible = value;
    }

    public void SetIsFileOperation(bool value)
    {
        Mode = value
            ? ProgressOperationMode.Stream
            : ProgressOperationMode.Standard;
    }

    public void SetIsBusy(bool value)
    {
        State = value
            ? ProgressState.Busy
            : ProgressState.Idle;
    }

    /// <summary>
    ///     Sets progress value of the ProgressBar to the maximal value
    /// </summary>
    public void PrgSetEnd()
    {
        PrgSet(-1);
    }

    /// <summary>
    ///     Increments current progress value of the ProgressBar
    /// </summary>
    /// <param name="addedValue">The added value.</param>
    public void PrgAdd(double addedValue = 1)
    {
        PrgSet(addedValue, null, ProgressChangeMode.Add);
    }

    /// <summary>
    ///     Adds value to the maximum of progress value.
    /// </summary>
    /// <param name="addedValue">The added value.</param>
    public void PrgMaxAdd(double addedValue)
    {
        PrgSet(null, addedValue, ProgressChangeMode.Add);
    }

    /// <summary>
    ///     Sets maximal allowed value of the ProgressBar and resets current progress
    /// </summary>
    /// <param name="max"></param>
    public void PrgSetMax(double max)
    {
        PrgSet(0, max);
    }

    public void Busy()
    {
        State = ProgressState.Busy;
    }

    public void Idle()
    {
        State = ProgressState.Idle;
    }

    protected override void OnExcludedPropertyChanged(string propertyName)
    {
        base.OnExcludedPropertyChanged(propertyName);
        _changedPropertiesSubject.OnNext(propertyName);
    }

    protected void RefreshIsPrgInfoVisible()
    {
        IsInfoVisible = Value < Maximum && !IsIndeterminate;
    }

    protected void UpdateProgressInfo()
    {
        if (Value == 0
            || !_stopwatch.IsRunning && Value < Maximum)
        {
            _stopwatch.Restart();
            CalculateProgressPercentage();
        }
        else if (Value.PreciseEquals(Maximum, 3))
        {
            _stopwatch.Reset();
            Info = string.Empty;
            //ProgressValue = 0;
            //ProgressMaximum = 0;

            if (Timer.IsRunning)
                Timer.TimerStop();

            CalculateProgressPercentage();
        }
        else if (Value > 0)
        {
            CalculateProgressPercentage();

            if (Mode != ProgressOperationMode.Stream)
            {
                double curr = _stopwatch.ElapsedMilliseconds;
                double avgMs = curr / Value;
                double etr = (Maximum - Value) / Value * curr;
                //double v = avgMs * ProgressBarValue / curr;

                if (!double.IsNaN(etr)
                    && !double.IsInfinity(etr))
                {
                    var est = $"ETR: {TimeSpan.FromMilliseconds(etr).GetTime()}";
                    var avg = $"AVG: {avgMs.GetTime()}";

                    //string speed = $"V: {v}x";
                    Info = $"{Math.Floor(Percentage * 100D)}% {Value}/{Maximum} {Unit} {est} {avg}"; // {speed}";
                }
            }
            else
            {
                double bytesReceived = Value;
                double totalBytesToReceive = Maximum;

                string curBt =
                    FileLengthConverter.ConvertFileLengthToString(bytesReceived,
                        LengthType.Bytes,
                        LengthType.AutoDetect);

                string curTb = FileLengthConverter.ConvertFileLengthToString(totalBytesToReceive,
                    LengthType.Bytes,
                    LengthType.AutoDetect);

                double curr = _stopwatch.Elapsed.TotalSeconds;

                string kbPerSec = FileLengthConverter.ConvertFileLengthToString(bytesReceived / curr,
                    LengthType.Bytes,
                    LengthType.AutoDetect,
                    1);

                double etr = 0;
                double perc = 0;

                if (bytesReceived > 0)
                {
                    etr = (totalBytesToReceive - bytesReceived) / bytesReceived * curr;
                    perc = Math.Floor(bytesReceived / totalBytesToReceive * 100D);
                }

                string est = etr > 0
                    ? $"ETR: {TimeSpan.FromSeconds(etr).GetTime()}"
                    : string.Empty;

                Info = $"{perc}% {curBt} /{curTb} {kbPerSec}/Sec {est}";
            }
        }
    }

    protected void CalculateProgressPercentage()
    {
        double pv = Value;
        double pm = Maximum;

        if (!(pv > 0)
            || !(pm > 0))
        {
            SetProgressPercentage(0);
        }
        else
        {
            double prc = pv / pm;

            if (!prc.PreciseEquals(Percentage))
                SetProgressPercentage(prc);
        }
    }

    private async Task ProcessChangesAsync(IList<Timestamped<IProgressChange>> newChangesbatch,
                                           CancellationToken cancellationToken)
    {
        await AddChangesAsync(newChangesbatch, cancellationToken);

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
            IList<Timestamped<IProgressChange>> changes = await DequeueAsync(cancellationToken);

            while (_changesQueue.Any())
            {
                foreach (Timestamped<IProgressChange> change in changes)
                    ProcessChange(change.Value);

                changes = await DequeueAsync(cancellationToken);
            }
        }
        finally
        {
            _progressLock.Release();
        }
    }

    private async Task<IList<Timestamped<IProgressChange>>> DequeueAsync(CancellationToken cancellationToken)
    {
        await _queueLock.WaitAsync(cancellationToken);

        try
        {
            var changes = _changesQueue.OrderBy(change => change.Timestamp).ToList();

            _changesQueue.Clear();

            return changes;
        }
        finally
        {
            _queueLock.Release();
        }
    }

    private async Task AddChangesAsync(IList<Timestamped<IProgressChange>> changes, CancellationToken cancellationToken)
    {
        await _queueLock.WaitAsync(cancellationToken);

        try
        {
            _changesQueue.AddRange(changes);
        }
        finally
        {
            _queueLock.Release();
        }
    }

    private void ProcessChange(IProgressChange progressChange)
    {
        switch (progressChange.PropertyName)
        {
            case nameof(IProgressAggregatorProperties.Value):
                ChangeProgressValue((ProgressChange<double>)progressChange);

                break;
            default:
                return;
        }
    }

    private void ProcessPrg(IProgressInfo progressInfo)
    {
        switch (progressInfo.ChangeMode)
        {
            case ProgressChangeMode.Add:
            {
                ProcessAddPrg(progressInfo.Value, progressInfo.Maximum);

                break;
            }
            case ProgressChangeMode.Set:
            {
                ProcessSetPrg(progressInfo.Value, progressInfo.Maximum);

                break;
            }
            case ProgressChangeMode.End:
                ProcessEndPrg();

                break;
            default:
                throw new ArgumentOutOfRangeException($"{progressInfo.ChangeMode} is not handled");
        }
    }

    private void ProcessEndPrg()
    {
        Value = Maximum;
    }

    private void ProcessSetPrg(double? value, double? maximum)
    {
        if (maximum is >= 0D)
            Maximum = maximum.Value;

        if (value is >= 0D
            && value.Value <= Maximum)
            Value = value.Value;
        else
            FExBasics.Logger.LogError("Invalid progress state");
    }

    private void ProcessAddPrg(double? value, double? maximum)
    {
        if (maximum is > 0D)
            Maximum += maximum.Value;

        if (value is > 0D
            && value + Value <= Maximum)
            Value += value.Value;
        else
            FExBasics.Logger.LogError("Invalid progress state");
    }

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            _changedPropertiesSubject?.Dispose();
            _subscriptions?.Dispose();
            _changeSubscription?.Dispose();
            _progressLock?.Dispose();
            _queueLock?.Dispose();
            _progressQueueLock?.Dispose();
            _progressChangeSubject?.Dispose();
        }

        _isDisposed = true;
    }
    #endregion
}