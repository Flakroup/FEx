using FEx.MVVM.Abstractions.Enums;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IProgressStatus : ILinkableNotifyPropertyChanged
{
    double Value { get; }
    double Maximum { get; }
    bool IsIndeterminate { get; }
    double Percentage { get; }
    double PrecisePercentage { get; }
    string Info { get; }
    string Unit { get; }
    ProgressOperationMode Mode { get; }
    ProgressState State { get; }
    bool IsBusy { get; }
    bool? IsInfoVisible { get; }
    string StatusInfo { get; }
    string CurrItemInfo { get; }
    string ThreadsInfo { get; }

    void SetValue(double value);
    void SetMaximum(double value);
    void SetIsIndeterminate(bool value);
    void SetPrecisePercentage(double value);
    void SetInfo(string value);
    void SetUnit(string value);
    void SetMode(ProgressOperationMode value);
    void SetIsFileOperation(bool value);
    void SetState(ProgressState value);
    void SetIsBusy(bool value);
    void Busy();
    void Idle();
    void SetIsInfoVisible(bool? value);
    void SetStatusInfo(string value);
    void SetCurrItemInfo(string value);
    void SetThreadsInfo(string value);
}