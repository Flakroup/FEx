using FEx.Abstractions.Interfaces;
using System;

namespace FEx.Abstractions.Flow;

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
    private readonly TData _data;
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
}