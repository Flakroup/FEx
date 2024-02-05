using FEx.MVVM.Enums;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IProgressAggregatorProperties
{
    double Value { get; }
    double Maximum { get; }
    bool IsIndeterminate { get; }
    double Percentage { get; }
    string Info { get; }
    string Unit { get; }
    ProgressOperationMode Mode { get; }
    ProgressState State { get; }
    bool IsBusy { get; }
    bool IsInfoVisible { get; }
}