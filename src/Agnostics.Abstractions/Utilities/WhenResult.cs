namespace FEx.Agnostics.Abstractions.Utilities;

public class WhenResult<T, TResult>
{
    // Set via the Result setter once a branch matches; IsResultSet guards reads before assignment.
    private TResult _result = default!;

    public TResult Result
    {
        get => _result;
        set
        {
            _result = value;
            IsResultSet = true;
        }
    }

    public bool IsResultSet { get; private set; }
    public T Value { get; }

    public WhenResult(T value)
    {
        Value = value;
    }
}