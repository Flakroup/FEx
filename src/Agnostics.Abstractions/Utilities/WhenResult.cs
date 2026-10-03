namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>Holds a value and the result of the first matching <c>When</c> branch in a fluent when/else chain.</summary>
/// <typeparam name="T">The tested value type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public class WhenResult<T, TResult>
{
    // Set via the Result setter once a branch matches; IsResultSet guards reads before assignment.
    private TResult _result = default!;

    /// <summary>Gets or sets the result; setting it marks the result as set.</summary>
    public TResult Result
    {
        get => _result;
        set
        {
            _result = value;
            IsResultSet = true;
        }
    }

    /// <summary>Gets a value indicating whether a branch has already matched.</summary>
    public bool IsResultSet { get; private set; }
    /// <summary>Gets the value that the branches test.</summary>
    public T Value { get; }

    /// <summary>Initializes the chain.</summary>
    /// <param name="value">The value to test.</param>
    public WhenResult(T value)
    {
        Value = value;
    }
}