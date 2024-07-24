using FEx.Abstractions.Interfaces;
using FEx.Common.Extensions;

namespace FEx.Abstractions.Flow;

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