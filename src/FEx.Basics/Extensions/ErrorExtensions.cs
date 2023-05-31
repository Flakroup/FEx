using FEx.Basics.Flow;
using FEx.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.Basics.Extensions;

public static class ErrorExtensions
{
    public static bool Match<TError>(this IError error, Func<TError, bool> action) where TError : IError
    {
        action.Guard(nameof(action));

        return error.InnerError is TError typedError && action.Invoke(typedError);
    }

    public static async Task<bool> MatchAsync<TError>(this IError error, Func<TError, Task<bool>> action)
        where TError : IError
    {
        action.Guard(nameof(action));

        return error.InnerError is TError typedError && await action.Invoke(typedError);
    }

    public static IError GetErrorRoot(this IError error) => error.InnerError is null
        ? error
        : error.InnerError.GetErrorRoot();

    public static bool TryGetError<TError>(this IError error, out TError foundError) where TError : class, IError
    {
        if (error is TError innerError)
        {
            foundError = innerError;
            return true;
        }

        if (error.InnerError is null)
        {
            foundError = null;
            return false;
        }

        return TryGetError(error.InnerError, out foundError);
    }

    public static TError Wrap<TError>(this Error errorToWrap) where TError : Error, new()
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
        new(error.InnerErrors.OfType<ExceptionError>().Select(x => x.Exception));
}