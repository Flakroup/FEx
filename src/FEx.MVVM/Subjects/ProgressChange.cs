using FEx.MVVM.Abstractions.Enums;

namespace FEx.MVVM.Subjects;

public record ProgressChange<T> : IProgressChange
{
    public string PropertyName { get; init; }
    public T Value { get; init; }
    public ProgressChangeMode ChangeMode { get; init; }
}