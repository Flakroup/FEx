using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces.Flow;

namespace FEx.Agnostics.Abstractions.Flow;

public abstract class ResultBase<TError> : IResult<TError> where TError : class, IError, new()
{
    public TError Error { get; }
    public bool IsSuccess => !IsFailure;
    public bool IsFailure { get; }

    protected ResultBase()
    {
    }

    protected ResultBase(TError error)
    {
        Error = error.Guard(nameof(Error));
        IsFailure = true;
    }
}