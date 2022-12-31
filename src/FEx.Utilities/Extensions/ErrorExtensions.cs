using FEx.Extensions;
using FEx.Utilities.Flow;
using System;
using System.Threading.Tasks;

namespace FEx.Utilities.Extensions;

public static class ErrorExtensions
{
    public static bool Match<TError>(this Error error, Func<TError, bool> action) where TError : Error
    {
        action.Guard(nameof(action));

        if (error.InnerError is TError typedError)
            return action.Invoke(typedError);

        return false;
    }

    public static async Task<bool> MatchAsync<TError>(this Error error, Func<TError, Task<bool>> action) where TError : Error
    {
        action.Guard(nameof(action));

        if (error.InnerError is TError typedError)
            return await action.Invoke(typedError);

        return false;
    }

    public static Error GetErrorRoot(this Error error)
    {
        if (error.InnerError is null)
            return error;

        return error.InnerError.GetErrorRoot();
    }

    public static bool TryGetError<TError>(this Error error, out TError foundError) where TError : Error
    {
        if (error is TError innerError)
        {
            foundError = innerError;
            return true;
        }

        if (error.InnerError == null)
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
}