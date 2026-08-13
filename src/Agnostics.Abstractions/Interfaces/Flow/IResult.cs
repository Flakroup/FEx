namespace FEx.Agnostics.Abstractions.Interfaces.Flow;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
}

public interface IResult<out TError> : IResult where TError : class, IError, new()
{
    TError? Error { get; }
}

public interface IResult<TData, out TError> : IResult<TError> where TError : class, IError, new()
{
    TData Data { get; }
    bool TryGetData(out TData data);
}