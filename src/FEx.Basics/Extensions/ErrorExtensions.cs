using FEx.Basics.Flow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Basics.Extensions;

public static class ErrorExtensions
{
    public static IError GetErrorRoot(this IError error) =>
        error.InnerError is null
            ? error
            : error.InnerError.GetErrorRoot();

    public static bool TryGetError<TError>(this IError error, out TError foundError) where TError : IError
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

        return TryGetError(error.InnerError, out foundError);
    }

    public static TError Wrap<TError>(this IError errorToWrap) where TError : class, IError, new()
    {
        var error = new TError();
        error.SetInnerError(errorToWrap);

        return error;
    }

    public static AggregatedError ToAggregatedError(this IError error) =>
        new(new List<IError>
        {
            error
        }.AsReadOnly());

    public static AggregateException ToAggregateException(this AggregatedError error) =>
        new(error.InnerErrors.OfType<IExceptionError>().Select(x => x.Exception));
}