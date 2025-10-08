using FEx.Agnostics.Abstractions.Utilities;
using System;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class WhenResultExtensions
{
    public static WhenResult<T, TResult> When<T, TResult>(this T value, Func<T, bool> predicate, TResult result)
    {
        if (value is not WhenResult<T, TResult> whenResult)
            whenResult = new(value);

        return whenResult.When(predicate, result);
    }

    public static WhenResult<T, TResult> When<T, TResult>(this WhenResult<T, TResult> whenResult,
                                                          Func<T, bool> predicate,
                                                          TResult result)
    {
        if (!whenResult.IsResultSet
            && predicate(whenResult.Value))
            whenResult.Result = result;

        return whenResult;
    }

    public static TResult Else<T, TResult>(this WhenResult<T, TResult> whenResult, TResult result) =>
        whenResult.IsResultSet
            ? whenResult.Result
            : result;
}