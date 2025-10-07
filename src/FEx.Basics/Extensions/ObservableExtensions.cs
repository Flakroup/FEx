using FEx.Abstractions.Flow;
using FEx.Abstractions.Flow.Errors;
using FEx.Rx.Extensions;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Basics.Extensions;

public static class ObservableExtensions
{
    /// <summary>
    /// Waits for the observable to retrieve a value and returns it wrapped in Result.
    /// </summary>
    /// <param name="observable">Observable to get the value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns>
    /// Data from observable wrapped in Result class.
    /// Result property IsSuccess is false if task was cancelled.
    /// </returns>
    public static async ValueTask<Result<T, Error>> GetResultAsync<T>(this IObservable<T> observable,
                                                                      CancellationToken cancellationToken = default)
    {
        Result<T, Error> result = observable.GetResult();

        if (result.IsSuccess)
            return result.Data;

        try
        {
            T data = await observable.FirstAsync().ToTaskAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return data;
        }
        catch (TaskCanceledException)
        {
            return Result<T, Error>.Failure;
        }
    }

    /// <summary>
    /// Tries to get the value from the observable and returns it wrapped in Result.
    /// </summary>
    /// <param name="observable">Observable to get the value.</param>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns>
    /// Data from observable wrapped in Result class.
    /// Result property IsSuccess is false if no value was present in observable.
    /// </returns>
    public static Result<T, Error> GetResult<T>(this IObservable<T> observable)
    {
        T result = default;
        var isSet = false;

        using IDisposable subscription = observable.Subscribe(x =>
        {
            result = x;
            isSet = true;
        });

        return !isSet
            ? Result<T, Error>.Failure
            : result;
    }
}