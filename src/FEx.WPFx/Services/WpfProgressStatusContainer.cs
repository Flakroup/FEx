using FEx.Agnostics.Abstractions.Extensions.Numericals;
using FEx.MVVM.Utilities;
using FEx.WPFx.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shell;

namespace FEx.WPFx.Services;

public class WpfProgressStatusContainer : ProgressAggregator, IWpfProgressStatusContainer
{
    private TaskbarItemProgressState _taskbarProgressState;
    private Visibility _prgInfoVisibility;

    public TaskbarItemProgressState TaskbarProgressState
    {
        get => _taskbarProgressState;
        set => SetProperty(ref _taskbarProgressState, value);
    }

    public Visibility PrgInfoVisibility
    {
        get => _prgInfoVisibility;
        set => SetProperty(ref _prgInfoVisibility, value);
    }

    public override void Report(string propertyName, object value)
    {
        base.Report(propertyName, value);

        switch (propertyName)
        {
            case nameof(IWpfProgressStatusContainer.TaskbarProgressState):
                SetPrgState((TaskbarItemProgressState)value);

                break;
        }
    }

    public override void SetIsIndeterminate(bool value)
    {
        base.SetIsIndeterminate(value);
        SetProgressState();
    }

    public override void SetPrecisePercentage(double value)
    {
        base.SetPrecisePercentage(value);
        SetProgressState();
    }

    public override void SetIsInfoVisible(bool? value)
    {
        base.SetIsInfoVisible(value);

        PrgInfoVisibility = !value.HasValue ? Visibility.Collapsed :
            value.Value ? Visibility.Visible : Visibility.Hidden;
    }

    public void SetPrgState(TaskbarItemProgressState value) => TaskbarProgressState = value;

    public override List<string> GetProperties()
    {
        List<string> properties = base.GetProperties();
        properties.Add(nameof(IWpfProgressStatusContainer.TaskbarProgressState));

        return properties;
    }

    public void SetProgressState() => TaskbarProgressState = IsIndeterminate ? TaskbarItemProgressState.Indeterminate :
            Value.PreciseEquals(Maximum, 3) ? TaskbarItemProgressState.None : TaskbarItemProgressState.Normal;
}
