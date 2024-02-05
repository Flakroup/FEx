using FEx.MVVM.Enums;

namespace FEx.MVVM.Subjects;

public interface IProgressChange
{
    string PropertyName { get; }
    ProgressChangeMode ChangeMode { get; }
}