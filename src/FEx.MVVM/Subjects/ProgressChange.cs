using FEx.MVVM.Abstractions.Enums;

namespace FEx.MVVM.Subjects;

public record ProgressChange<T> : IProgressChange
{
    public string PropertyName { get; init; } = string.Empty;

    // Data record always populated via object initializer at the emit site.
    public T Value { get; init; } = default!;
    public ProgressChangeMode ChangeMode { get; init; }
}