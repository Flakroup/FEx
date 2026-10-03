using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System;

namespace FEx.Agnostics.Abstractions.Flow;

/// <summary>Result of an operation without data.</summary>
/// <typeparam name="TError">The error type.</typeparam>
public class Result<TError> : ResultBase<TError> where TError : class, IError, new()
{
    /// <summary>Gets a successful result.</summary>
    public static Result<TError> Success => new();

    /// <summary>Gets a failed result with a new default error.</summary>
    public static Result<TError> Failure => new(new());

    /// <summary>Initializes a successful result.</summary>
    public Result()
    {
    }

    /// <summary>Initializes a failed result.</summary>
    /// <param name="error">The error that caused the failure.</param>
    public Result(TError error)
        : base(error)
    {
    }

    /// <summary>Converts an error to a failed result.</summary>
    /// <param name="error">The error that caused the failure.</param>
    public static implicit operator Result<TError>(TError error) => new(error);
}

/// <summary>Result of an operation that returns data.</summary>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
public class Result<TData, TError> : ResultBase<TError>, IResult<TData, TError> where TError : class, IError, new()
{
    // Only read via Data/TryGetData, both guarded by IsSuccess; unset in the failure state (throw-guarded invariant).
    private readonly TData _data = default!;
    /// <summary>Gets a failed result with a new default error.</summary>
    public static Result<TData, TError> Failure => new(new TError());

    /// <summary>Gets the data of a successful result.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public TData Data =>
        IsSuccess
            ? _data
            : throw new InvalidOperationException("Cannot access data in failed state");

    /// <summary>Initializes a successful result.</summary>
    /// <param name="data">The result data.</param>
    public Result(TData data)
    {
        _data = data;
    }

    /// <summary>Initializes a failed result.</summary>
    /// <param name="error">The error that caused the failure.</param>
    public Result(TError error)
        : base(error)
    {
    }

    /// <inheritdoc />
    public bool TryGetData(out TData data)
    {
        data = _data;

        return IsSuccess;
    }

    /// <summary>Converts data to a successful result.</summary>
    /// <param name="data">The result data.</param>
    public static implicit operator Result<TData, TError>(TData data) => new(data);

    /// <summary>Converts an error to a failed result.</summary>
    /// <param name="error">The error that caused the failure.</param>
    public static implicit operator Result<TData, TError>(TError error) => new(error);

    /// <summary>Creates a successful result.</summary>
    /// <param name="data">The result data.</param>
    /// <returns>A successful result holding <paramref name="data" />.</returns>
    public static Result<TData, TError> Success(TData data) => data;
}