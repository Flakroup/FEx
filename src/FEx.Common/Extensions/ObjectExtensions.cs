using JetBrains.Annotations;
using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace FEx.Common.Extensions;

public static class ObjectExtensions
{
    /// <summary>
    ///     Guards if provided value is not null, otherwise
    ///     throws an exception of type <see cref="ArgumentNullException" /> with a specific
    /// <paramref name="message" />
    /// when the precondition has not been met
    /// </summary>
    /// <typeparam name="T">Current type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">Name of the parameter.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>
    ///     The value itself.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    ///     Throws a <see cref="ArgumentNullException" /> when <paramref name="value" /> is a null reference.
    /// </remarks>
    public static T Guard<T>([CanBeNull] this T value,
                             [CallerMemberName] string paramName = null,
                             string message = null) =>
        value.Guard(v => v is null, paramName, message);

    /// <summary>
    ///     Guards the specified <paramref name="predicate" /> from being violated by
    ///     throwing an exception of type <see cref="ArgumentNullException" /> with a specific
    /// <paramref name="message" />
    /// when the precondition has not been met
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="value">Value to check.</param>
    /// <param name="predicate">The precondition that has to be met</param>
    /// <param name="paramName">Name of the parameter.</param>
    /// <param name="message">The message that will be included in the exception</param>
    /// <returns>
    ///     The value itself.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    ///     Throws a <see cref="ArgumentNullException" /> if the condition is false.
    /// </remarks>
    public static T Guard<T>([CanBeNull] this T value,
                             Func<T, bool> predicate,
                             [CallerMemberName] string paramName = null,
                             string message = null)
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        if (predicate(value))
            throw new ArgumentNullException(paramName, message ?? "Precondition not met.");

        return value;
    }

    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmpty(this object data) =>
        data switch
        {
            string stringValue => string.IsNullOrEmpty(stringValue),
            ICollection collection => collection.Count == 0,
            IEnumerable enumerable => !enumerable.Any(),
            null => true,
            _ => false
        };
}