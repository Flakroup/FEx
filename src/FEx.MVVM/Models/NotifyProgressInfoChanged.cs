using FEx.Agnostics.BaseObjects;
using FEx.MVVM.Abstractions.Enums;

namespace FEx.MVVM.Models;

public class NotifyProgressInfoChanged : NotifyPropertyChanged
{
    private ProgressChangeMode _progressMode;
    private double? _progressValue;
    private double? _progressMaximum;

    public double? Value
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public double? Maximum
    {
        get => _progressMaximum;
        set => SetProperty(ref _progressMaximum, value);
    }

    public ProgressChangeMode ChangeMode
    {
        get => _progressMode;
        set => SetProperty(ref _progressMode, value);
    }
}