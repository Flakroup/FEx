namespace FEx.Agnostics.Abstractions.Utilities;

public class WhenResult<T, TResult>
{
    private TResult _result;

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