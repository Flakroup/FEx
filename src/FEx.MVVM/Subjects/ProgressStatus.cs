using FEx.Agnostics.Abstractions.Extensions.Numericals;
using FEx.Core.Collections.Concurrent;
using FEx.MVVM.Abstractions;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Extensions;
using System;

namespace FEx.MVVM.Subjects;

public class ProgressStatus : LinkableNotifyPropertyChanged, IProgressStatus
{
    private double _value;
    private double _maximum;
    private bool _isIndeterminate;
    private double _percentage;
    private double _precisePercentage;
    private string _prgUnit;
    private string _info;
    private ProgressOperationMode _mode;
    private ProgressState _state;
    private bool _isBusy;
    private bool? _isInfoVisible;
    private string _currItemInfo;
    private string _statusInfo;
    private string _threadsInfo;

    public double Value
    {
        get => _value;
        protected set => SetProperty(ref _value, value, OnValueChanged);
    }

    public double Maximum
    {
        get => _maximum;
        protected set => SetProperty(ref _maximum, value, OnMaximumChanged);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        protected set => SetProperty(ref _isIndeterminate, value, OnIsIndeterminateChanged);
    }

    public double Percentage
    {
        get => _percentage;
        private set => SetProperty(ref _percentage, value);
    }

    public double PrecisePercentage
    {
        get => _precisePercentage;
        protected set => SetProperty(ref _precisePercentage, value, prc => Percentage = Math.Floor(prc * 100D));
    }

    public string Info
    {
        get => _info;
        protected set => SetProperty(ref _info, value);
    }

    public string Unit
    {
        get => _prgUnit;
        protected set => SetProperty(ref _prgUnit, value);
    }

    public ProgressOperationMode Mode
    {
        get => _mode;
        protected set => SetProperty(ref _mode, value);
    }

    public ProgressState State
    {
        get => _state;
        protected set => SetProperty(ref _state, value, OnStateChanged);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool? IsInfoVisible
    {
        get => _isInfoVisible;
        protected set => SetProperty(ref _isInfoVisible, value);
    }

    public string StatusInfo
    {
        get => _statusInfo;
        protected set => SetProperty(ref _statusInfo, value, OnStatusInfoChanged);
    }

    public string CurrItemInfo
    {
        get => _currItemInfo;
        protected set => SetProperty(ref _currItemInfo, value);
    }

    public string ThreadsInfo
    {
        get => _threadsInfo;
        protected set => SetProperty(ref _threadsInfo, value);
    }

    protected ConcurrentHashSet<string> ExcludedProperties { get; }

    public ProgressStatus()
    {
        //todo reduce to only these delayed ones
        ExcludedProperties = new(ProgressAggregatorExtensions.ListenerPropertyNames);
    }

    public override void OnPropertyChanged(string propertyName = null)
    {
        if (ExcludedProperties.Contains(propertyName))
        {
            OnExcludedPropertyChanged(propertyName);

            return;
        }

        InvokePropertyChanged(propertyName);
    }

    protected virtual void InvokePropertyChanged(string propertyName) => base.OnPropertyChanged(propertyName);

    protected virtual void OnStateChanged(ProgressState state) => IsBusy = state == ProgressState.Busy;

    protected virtual void OnValueChanged(double value)
    {
        if (IsIndeterminate && value > 0)
            IsIndeterminate = false;

        RefreshIsPrgInfoVisible();
        CalculateProgressPercentage();
    }

    protected virtual void OnMaximumChanged(double value)
    {
        RefreshIsPrgInfoVisible();
        CalculateProgressPercentage();
    }

    protected virtual void OnIsIndeterminateChanged(bool value) => RefreshIsPrgInfoVisible();

    protected virtual void OnExcludedPropertyChanged(string propertyName)
    {
    }

    protected virtual void RefreshIsPrgInfoVisible() => IsInfoVisible = Value < Maximum && !IsIndeterminate;

    protected virtual void CalculateProgressPercentage()
    {
        var pv = Value;
        var pm = Maximum;

        if (pm <= 0)
        {
            PrecisePercentage = 0;

            return;
        }

        if (pv <= 0)
        {
            PrecisePercentage = 0;

            return;
        }

        var prc = pv / pm;

        if (prc.PreciseEquals(PrecisePercentage, 3))
            return;

        PrecisePercentage = prc;
    }

    protected virtual void OnStatusInfoChanged(string value)
    {
    }

    #region Setters
    public virtual void SetValue(double value) => Value = value;
    public virtual void SetMaximum(double value) => Maximum = value;
    public virtual void SetIsIndeterminate(bool value) => IsIndeterminate = value;
    public virtual void SetPrecisePercentage(double value) => PrecisePercentage = value;
    public virtual void SetInfo(string value) => Info = value;
    public virtual void SetUnit(string value) => Unit = value;
    public virtual void SetMode(ProgressOperationMode value) => Mode = value;

    public virtual void SetIsFileOperation(bool value) =>
        SetMode(value
            ? ProgressOperationMode.Stream
            : ProgressOperationMode.Standard);

    public virtual void SetState(ProgressState value) => State = value;

    public virtual void SetIsBusy(bool value) =>
        SetState(value
            ? ProgressState.Busy
            : ProgressState.Idle);

    public virtual void Busy() => State = ProgressState.Busy;
    public virtual void Idle() => State = ProgressState.Idle;
    public virtual void SetIsInfoVisible(bool? value) => IsInfoVisible = value;
    public virtual void SetStatusInfo(string value) => StatusInfo = value;
    public virtual void SetCurrItemInfo(string value) => CurrItemInfo = value;
    public virtual void SetThreadsInfo(string value) => ThreadsInfo = value;
    #endregion
}