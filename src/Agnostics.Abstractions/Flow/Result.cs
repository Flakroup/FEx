using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System;

namespace FEx.Agnostics.Abstractions.Flow;

public class Result<TError> : ResultBase<TError> where TError : class, IError, new()
{
    public static Result<TError> Success => new();

    public static Result<TError> Failure => new(new());

    public Result()
    {
    }

    public Result(TError error)
        : base(error)
    {
    }

    public static implicit operator Result<TError>(TError error) => new(error);
}

public class Result<TData, TError> : ResultBase<TError>, IResult<TData, TError> where TError : class, IError, new()
{
    // Only read via Data/TryGetData, both guarded by IsSuccess; unset in the failure state (throw-guarded invariant).
    private readonly TData _data = default!;
    public static Result<TData, TError> Failure => new(new TError());

    public TData Data =>
        IsSuccess
            ? _data
            : throw new InvalidOperationException("Cannot access data in failed state");

    public Result(TData data)
    {
        _data = data;
    }

    public Result(TError error)
        : base(error)
    {
    }

    public bool TryGetData(out TData data)
    {
        data = _data;

        return IsSuccess;
    }

    public static implicit operator Result<TData, TError>(TData data) => new(data);

    public static implicit operator Result<TData, TError>(TError error) => new(error);

    public static Result<TData, TError> Success(TData data) => data;
}