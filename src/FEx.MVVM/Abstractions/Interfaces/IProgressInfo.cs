using FEx.MVVM.Enums;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IProgressInfo
{
    double? Value { get; }
    double? Maximum { get; }
    ProgressChangeMode ChangeMode { get; }
}