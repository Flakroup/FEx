using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Numericals;
using FEx.Agnostics.Abstractions.Logging;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Abstractions.Subjects;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Events;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Subjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace FEx.MVVM.Utilities;

public class ProgressAggregator : ProgressStatus, IProgressAggregator
{
    protected readonly FExSubject<string> _changedPropertiesSubject;
    protected readonly CompositeDisposable _subscriptions;

    private bool _isDisposed;

    public event ProgressPropertyChangedEventHandler ProgressPropertyChanged;
    public Stopwatch Stopwatch { get; }

    public IFExTimer Timer { get; }

    public string Id { get; }

    public ProgressAggregator()
    {
        Id = Guid.NewGuid().ToString();
        Stopwatch = new();
        Timer = new FExTimer().WithCallback(TimerCallback);
        _changedPropertiesSubject = new();
        _subscriptions = [];

        //todo if needed Dispose and renew sub on progress Start/End
        _changedPropertiesSubject.Where(ExcludedProperties.Contains)
            .Buffer(FExMvvm.DefaultUIRefreshInterval)
            .Where(static propertyNames => propertyNames.Count > 0)
            .Select(static propertyNames => propertyNames.Distinct().ToList())
            .AsyncSubscribe(OnExcludedPropertiesChanged, _subscriptions);
    }

    public override bool SetProperty<TRet>(ref TRet backingField,
                                           TRet newValue,
                                           Action<TRet> onPropertyChanged = null,
                                           [CallerMemberName] string propertyName = null)
    {
        if (!base.SetProperty(ref backingField, newValue, onPropertyChanged, propertyName))
            return false;

        InvokeProgressPropertyChanged(newValue, propertyName);

        return true;
    }

    public virtual void Start()
    {
        IsIndeterminate = true;
        CurrItemInfo = string.Empty;
        StatusInfo = string.Empty;
        Info = string.Empty;
    }

    public virtual void Stop()
    {
        IsIndeterminate = false;
        Value = 0;
        Maximum = 0;
        StatusInfo = string.Empty;
        CurrItemInfo = string.Empty;
        ThreadsInfo = string.Empty;
        Unit = string.Empty;
        Info = string.Empty;
    }

    public virtual void Report(string propertyName, object value)
    {
        switch (propertyName)
        {
            case nameof(IProgressAggregator.StatusInfo):
                StatusInfo = (string)value;

                break;
            case nameof(IProgressAggregator.CurrItemInfo):
                CurrItemInfo = (string)value;

                break;
            case nameof(IProgressAggregator.Unit):
                Unit = (string)value;

                break;
            case nameof(IProgressAggregator.Info):
                Info = (string)value;

                break;
            case nameof(IProgressAggregator.IsIndeterminate):
                IsIndeterminate = (bool)value;

                break;
            case nameof(IProgressAggregator.Mode):
                Mode = (ProgressOperationMode)value;

                break;
            case nameof(IProgressAggregator.Maximum):
                Maximum = (double)value;

                break;
            case nameof(IProgressAggregator.Value):
                Value = (double)value;

                break;
            case nameof(IProgressAggregator.PrecisePercentage):
                PrecisePercentage = (double)value;

                break;
            case nameof(IProgressAggregator.IsInfoVisible):
                IsInfoVisible = (bool?)value;

                break;
            case nameof(IProgressAggregator.ThreadsInfo):
                ThreadsInfo = (string)value;

                break;
            case nameof(IProgressAggregator.State):
                State = (ProgressState)value;

                break;
        }
    }

    public virtual List<string> GetProperties() =>
    [
        nameof(IProgressStatus.Value),
        nameof(IProgressStatus.Maximum),
        nameof(IProgressStatus.IsIndeterminate),
        nameof(IProgressStatus.Percentage),
        nameof(IProgressStatus.PrecisePercentage),
        nameof(IProgressStatus.Info),
        nameof(IProgressStatus.Unit),
        nameof(IProgressStatus.Mode),
        nameof(IProgressStatus.IsBusy),
        nameof(IProgressStatus.IsInfoVisible),
        nameof(IProgressStatus.StatusInfo),
        nameof(IProgressStatus.CurrItemInfo),
        nameof(IProgressStatus.ThreadsInfo),
        nameof(IProgressStatus.State)
    ];

    protected virtual void LogError(string message) => FExStaticLogger.Error(message);

    protected virtual void ProcessEndPrg() => Value = Maximum;

    protected virtual void ProcessSetPrg(double? value, double? maximum)
    {
        if (maximum is >= 0D)
            Maximum = maximum.Value;

        if (value is >= 0D
            && value.Value <= Maximum)
            Value = value.Value;
        else
            LogError("Invalid progress state");
    }

    protected virtual void ProcessAddPrg(double? value, double? maximum)
    {
        if (maximum is > 0D)
            Maximum += maximum.Value;

        if (value is > 0D
            && value + Value <= Maximum)
            Value += value.Value;
        else
            LogError("Invalid progress state");
    }

    protected virtual void TimerCallback()
    {
        if (!Stopwatch.IsRunning
            && Value < Maximum)
        {
            Stopwatch.Restart();
            UpdateProgressInfo();
        }
        else if (Value.PreciseEquals(Maximum, 3))
        {
            if (Timer.IsRunning)
                Timer.Stop();

            Stopwatch.Reset();
            Info = string.Empty;
            IsIndeterminate = false;
        }
        else
        {
            UpdateProgressInfo();
        }
    }

    protected virtual void UpdateProgressInfo()
    {
        TimeSpan elapsed = Stopwatch.Elapsed;
        double elapsedMilliseconds = elapsed.TotalMilliseconds;
        double maximum = Maximum;
        double value = Value;
        double percentage = Percentage;
        double avgMs = elapsedMilliseconds / value;

        double etr = value > 0
            ? (maximum - value) * avgMs
            : 0;

        string est = !double.IsNaN(etr) && !double.IsInfinity(etr) && etr > 0
            ? $"ETR: {TimeSpan.FromMilliseconds(etr).GetTime()}"
            : string.Empty;

        switch (Mode)
        {
            case ProgressOperationMode.Standard:
            {
                string avg = est.IsNotNullOrEmptyString()
                    ? $"AVG: {avgMs.GetTime()}"
                    : string.Empty;

                Info =
                    $"{percentage}% {value}/{maximum}{(Unit.IsNotNullOrEmptyString() ? $"{Unit}" : string.Empty)} {est} {avg}";

                break;
            }
            case ProgressOperationMode.Stream:
            {
                string curBt =
                    FileLengthConverter.ConvertFileLengthToString(value, LengthType.Bytes, LengthType.AutoDetect);

                string curTb = FileLengthConverter.ConvertFileLengthToString(maximum,
                    LengthType.Bytes,
                    LengthType.AutoDetect);

                double curr = elapsed.TotalSeconds;

                string kbPerSec = FileLengthConverter.ConvertFileLengthToString(value / curr,
                    LengthType.Bytes,
                    LengthType.AutoDetect,
                    1);

                Info = $"{percentage}% {curBt}/{curTb} {kbPerSec}/sec {est}";

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(Mode), $"{Mode} is not handled");
        }
    }

    protected override void OnExcludedPropertyChanged(string propertyName)
    {
        base.OnExcludedPropertyChanged(propertyName);
        _changedPropertiesSubject.OnNext(propertyName);
    }

    protected void InvokeProgressPropertyChanged<TRet>(TRet newValue, string propertyName)

    {
        if (ProgressPropertyChanged is null)
            return;

        FExCoreStatics.Dispatcher.InvokeOnMainThread(EventDelegate, this);

        return;

        void EventDelegate() => InvokeProgressPropertyChanged(new(Id, propertyName, newValue));
    }

    private void OnExcludedPropertiesChanged(IEnumerable<string> propertyNames)
    {
        foreach (string propertyName in propertyNames)
            InvokePropertyChanged(propertyName);
    }

    private void InvokeProgressPropertyChanged(ProgressPropertyChangedEventArgs args)
    {
        if (args is null)
            return;

        ProgressPropertyChanged?.Invoke(this, args);
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
        }

        _isDisposed = true;
    }
    #endregion

    #region Setters
    /// <summary>
    /// Sets current progress value and maximal allowed value of the ProgressBar
    /// </summary>
    /// <param name="value">Progress value to be added or set</param>
    /// <param name="maximum">Maximal allowed value.</param>
    /// <param name="mode">Progress change mode. ProgressChangeMode.End sets ProgressValue to current ProgressMaximum.</param>
    public virtual void PrgSet(double? value, double? maximum = null, ProgressChangeMode mode = ProgressChangeMode.Set)
    {
        if (!Timer.IsRunning
            && (maximum.HasValue || value.HasValue)
            && mode != ProgressChangeMode.End)
            Timer.Start();

        switch (mode)
        {
            case ProgressChangeMode.Set:
                ProcessSetPrg(value, maximum);

                break;
            case ProgressChangeMode.Add:
                ProcessAddPrg(value, maximum);

                break;
            case ProgressChangeMode.End:
                ProcessEndPrg();

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    /// <summary>
    /// Sets progress value of the ProgressBar to the maximal value
    /// </summary>
    public void PrgSetEnd() => PrgSet(-1, mode: ProgressChangeMode.End);

    /// <summary>
    /// Increments current progress value of the ProgressBar
    /// </summary>
    /// <param name="addedValue">The added value.</param>
    public void PrgAdd(double addedValue = 1) => PrgSet(addedValue, mode: ProgressChangeMode.Add);

    /// <summary>
    /// Adds value to the maximum of progress value.
    /// </summary>
    /// <param name="addedValue">The added value.</param>
    public void PrgMaxAdd(double addedValue) => PrgSet(null, addedValue, ProgressChangeMode.Add);

    /// <summary>
    /// Sets maximal allowed value of the ProgressBar and resets current progress
    /// </summary>
    /// <param name="max"></param>
    public void PrgSetMax(double max) => PrgSet(0, max);
    #endregion
}