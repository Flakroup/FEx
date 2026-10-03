using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces.Flow;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for navigating and converting error chains.</summary>
public static class ErrorExtensions
{
    /// <summary>Gets the innermost error of a chain.</summary>
    /// <param name="error">The error to start from.</param>
    /// <returns>The last inner error, or the error itself when it has none.</returns>
    public static IError GetErrorRoot(this IError error) =>
        error.InnerError is null
            ? error
            : error.InnerError.GetErrorRoot();

    /// <summary>Finds the first error of a given type in a chain, starting with the error itself.</summary>
    /// <typeparam name="TError">The error type to look for.</typeparam>
    /// <param name="error">The error to start from.</param>
    /// <param name="foundError">Receives the first matching error.</param>
    /// <returns><c>true</c> if a matching error was found.</returns>
    public static bool TryGetError<TError>(this IError error, [MaybeNullWhen(false)] out TError foundError) where TError : IError
    {
        if (error is TError innerError)
        {
            foundError = innerError;

            return true;
        }

        if (error.InnerError is null)
        {
            foundError = default;

            return false;
        }

        return error.InnerError.TryGetError(out foundError);
    }

    /// <summary>Creates a new error that has the given error as its inner error.</summary>
    /// <typeparam name="TError">The type of the wrapping error.</typeparam>
    /// <param name="errorToWrap">The error to wrap.</param>
    /// <returns>The new wrapping error.</returns>
    public static TError Wrap<TError>(this IError errorToWrap) where TError : class, IError, new()
    {
        var error = new TError();
        error.SetInnerError(errorToWrap);

        return error;
    }

    /// <summary>Wraps an error in an aggregated error.</summary>
    /// <param name="error">The error to wrap.</param>
    /// <returns>An aggregated error containing the single error.</returns>
    public static AggregatedError ToAggregatedError(this IError error) =>
        new(new List<IError>
        {
            error
        }.AsReadOnly());

    /// <summary>Converts an aggregated error to an aggregate exception built from the exceptions of its inner exception errors.</summary>
    /// <param name="error">The aggregated error to convert.</param>
    /// <returns>An aggregate exception holding the inner exceptions.</returns>
    public static AggregateException ToAggregateException(this AggregatedError error) =>
        new(error.InnerErrors.OfType<IExceptionError>().Select(static x => x.Exception).OfType<Exception>());
}