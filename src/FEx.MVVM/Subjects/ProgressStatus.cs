using FEx.Basics.Abstractions;
using FEx.Basics.Collections.Concurrent;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Enums;

namespace FEx.MVVM.Subjects;

public class ProgressStatus : NotifyPropertyChanged, IProgressAggregatorProperties
{
    private double _value;
    private double _maximum;
    private bool _isIndeterminate;
    private double _percentage;
    private string _prgUnit;
    private string _info;
    private ProgressOperationMode _mode;
    private ProgressState _state;
    private bool _isBusy;
    private bool _isInfoVisible;

    public double Value
    {
        get => _value;
        protected set => SetProperty(ref _value, value);
    }

    public double Maximum
    {
        get => _maximum;
        protected set => SetProperty(ref _maximum, value);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        protected set => SetProperty(ref _isIndeterminate, value);
    }

    public double Percentage
    {
        get => _percentage;
        protected set => SetProperty(ref _percentage, value);
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
        protected set => SetProperty(ref _state, value, state => IsBusy = state == ProgressState.Busy);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsInfoVisible
    {
        get => _isInfoVisible;
        protected set => SetProperty(ref _isInfoVisible, value);
    }

    protected ConcurrentHashSet<string> ExcludedProperties { get; }

    public ProgressStatus()
    {
        ExcludedProperties =
        [
            nameof(Value),
            nameof(Maximum),
            nameof(IsIndeterminate),
            nameof(Percentage),
            nameof(Info),
            nameof(Unit),
            nameof(Mode),
            nameof(State),
            nameof(IsBusy),
            nameof(IsInfoVisible)
        ];
    }

    public override void OnPropertyChanged(string propertyName = null)
    {
        if (ExcludedProperties.Contains(propertyName))
        {
            OnExcludedPropertyChanged(propertyName);

            return;
        }

        base.OnPropertyChanged(propertyName);
    }

    protected virtual void OnExcludedPropertyChanged(string propertyName)
    {
    }
}