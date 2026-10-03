namespace FEx.Agnostics.Abstractions.Interfaces.Flow;

/// <summary>Outcome of an operation that either succeeded or failed.</summary>
public interface IResult
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    bool IsSuccess { get; }
    /// <summary>Gets a value indicating whether the operation failed.</summary>
    bool IsFailure { get; }
}

/// <summary>Outcome of an operation that carries an error when it failed.</summary>
/// <typeparam name="TError">The error type.</typeparam>
public interface IResult<out TError> : IResult where TError : class, IError, new()
{
    /// <summary>Gets the error of a failed operation, or null when it succeeded.</summary>
    TError? Error { get; }
}

/// <summary>Outcome of an operation that carries data when it succeeded and an error when it failed.</summary>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
public interface IResult<TData, out TError> : IResult<TError> where TError : class, IError, new()
{
    /// <summary>Gets the data of a successful operation.</summary>
    TData Data { get; }
    /// <summary>Gets the data when the operation succeeded.</summary>
    /// <param name="data">Receives the data, or the default value when the operation failed.</param>
    /// <returns><c>true</c> if the operation succeeded.</returns>
    bool TryGetData(out TData data);
}