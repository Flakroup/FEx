using FEx.Extensions;

namespace FEx.Basics.Flow;

public class Result<TError> where TError : class, IError, new()
{
    public static Result<TError> Success => new();

    public static Result<TError> Failure => new(new TError());

    public static implicit operator Result<TError>(TError error) => new(error);

    public TError Error { get; }
    public bool IsSuccess => !IsFailure;
    public bool IsFailure { get; }

    public Result()
    {
    }

    public Result(TError error)
    {
        Error = error.Guard(nameof(Error));
        IsFailure = true;
    }
}

public class Result<TData, TError> where TError : class, IError, new()
{
    public static Result<TData, TError> Failure => new(new TError());

    public static implicit operator Result<TData, TError>(TData data) => new(data);

    public static implicit operator Result<TData, TError>(TError error) => new(error);

    public TError Error { get; }
    public bool IsSuccess => !IsFailure;
    public bool IsFailure { get; }

    public TData Data { get; }

    public Result(TData data)
    {
        Data = data;
    }

    public Result(TError error)
    {
        Error = error.Guard(nameof(Error));
        IsFailure = true;
    }
}