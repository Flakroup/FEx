using FEx.MVVM.Abstractions.Enums;

namespace FEx.MVVM.Subjects;

public interface IProgressChange
{
    string PropertyName { get; }
    ProgressChangeMode ChangeMode { get; }
}