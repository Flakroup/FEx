using System;

namespace FEx.Utilities.Flow;

public class Result<TError> where TError : IError, new()
{
    public static Result<TError> Success => new();
    public static Result<TError> Failure => new(new());

    public TError Error { get; }
    public bool IsSuccessful { get; }
    public bool IsFailure => !IsSuccessful;

    public Result()
    {
        IsSuccessful = true;
    }

    public Result(TError error)
    {
        Error = error;
        IsSuccessful = false;
    }

    public static implicit operator Result<TError>(TError right)
    {
        return new(right);
    }

    public static implicit operator Result<TError>(string message)
    {
        TError error = Activator.CreateInstance<TError>();
        error.Message = message;
        return new(error);
    }
}

public class Result<TData, TError> : Result<TError> where TError : Error, new()
{
    public new static Result<TData, TError> Failure => new(new TError());

    public TData Data { get; }

    public Result(TData data)
    {
        Data = data;
    }

    public Result(TError error)
        : base(error)
    {
    }

    public static implicit operator Result<TData, TError>(TData data)
    {
        return new(data);
    }

    public static implicit operator Result<TData, TError>(TError error)
    {
        return new(error);
    }
}