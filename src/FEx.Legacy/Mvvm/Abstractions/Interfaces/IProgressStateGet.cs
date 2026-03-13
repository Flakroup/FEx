namespace FEx.Legacy.Mvvm.Abstractions.Interfaces;

public interface IProgressStateGet
{
    bool IsFileOperation { get; }
    bool? IsPrgInfoVisible { get; }
    bool IsBusy { get; }
}