using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces.Flow;

namespace FEx.Agnostics.Abstractions.Flow;

/// <summary>Base implementation of <see cref="IResult{TError}" /> that is successful unless constructed with an error.</summary>
/// <typeparam name="TError">The error type.</typeparam>
public abstract class ResultBase<TError> : IResult<TError> where TError : class, IError, new()
{
    /// <inheritdoc />
    public TError? Error { get; }
    /// <inheritdoc />
    public bool IsSuccess => !IsFailure;
    /// <inheritdoc />
    public bool IsFailure { get; }

    /// <summary>Initializes a successful result.</summary>
    protected ResultBase()
    {
    }

    /// <summary>Initializes a failed result.</summary>
    /// <param name="error">The error that caused the failure.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="error" /> is null.</exception>
    protected ResultBase(TError error)
    {
        Error = error.Guard(nameof(Error));
        IsFailure = true;
    }
}