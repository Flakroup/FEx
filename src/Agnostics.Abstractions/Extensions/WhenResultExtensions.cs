using FEx.Agnostics.Abstractions.Utilities;
using System;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for a fluent when/else chain that picks the result of the first matching condition.</summary>
public static class WhenResultExtensions
{
    /// <summary>Starts a chain with the first condition</summary>
    /// <typeparam name="T">The tested value type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="value">The value to test; an existing chain is continued when it already is one.</param>
    /// <param name="predicate">The condition.</param>
    /// <param name="result">The result when the condition holds.</param>
    /// <returns>The chain.</returns>
    public static WhenResult<T, TResult> When<T, TResult>(this T value, Func<T, bool> predicate, TResult result)
    {
        if (value is not WhenResult<T, TResult> whenResult)
            whenResult = new(value);

        return whenResult.When(predicate, result);
    }

    /// <summary>Adds a condition that is evaluated only when no earlier condition matched</summary>
    /// <typeparam name="T">The tested value type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="whenResult">The chain.</param>
    /// <param name="predicate">The condition.</param>
    /// <param name="result">The result when the condition holds.</param>
    /// <returns>The same chain.</returns>
    public static WhenResult<T, TResult> When<T, TResult>(this WhenResult<T, TResult> whenResult,
                                                          Func<T, bool> predicate,
                                                          TResult result)
    {
        if (!whenResult.IsResultSet
            && predicate(whenResult.Value))
            whenResult.Result = result;

        return whenResult;
    }

    /// <summary>Ends the chain</summary>
    /// <typeparam name="T">The tested value type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="whenResult">The chain.</param>
    /// <param name="result">The fallback result.</param>
    /// <returns>The matched result, or <paramref name="result" /> when no condition matched.</returns>
    public static TResult Else<T, TResult>(this WhenResult<T, TResult> whenResult, TResult result) =>
        whenResult.IsResultSet
            ? whenResult.Result
            : result;
}