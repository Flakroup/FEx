using JetBrains.Annotations;
using System;
using System.Runtime.CompilerServices;
using SysNotNull = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class GuardExtensions
{
    /// <summary>
    /// Guards if provided value is not null, otherwise
    /// throws an exception of type <see cref="ArgumentNullException" /> with a specific
    /// <paramref name="message" />
    /// when the precondition has not been met
    /// </summary>
    /// <typeparam name="T">Current type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="paramName">Name of the parameter.</param>
    /// <returns>
    /// The value itself.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    /// Throws a <see cref="ArgumentNullException" /> when <paramref name="value" /> is a null reference.
    /// </remarks>
    [ContractAnnotation("value: null => stop")]
    [return: SysNotNull]
#pragma warning disable S2360 // Optional parameter overload not possible - CallerMemberName requires optional string, creating ambiguity with message parameter
    public static T GuardProperty<T>([SysNotNull] [CanBeNull] this T value,
                                     string? message = null,
                                     [CallerMemberName] string? paramName = null)
#pragma warning restore S2360
    {
        if (value is null)
            throw new ArgumentNullException(paramName, message ?? "Precondition not met.");

        return value;
    }

    /// <summary>
    /// Guards if provided value is not null, otherwise
    /// throws an exception of type <see cref="ArgumentNullException" /> with a specific
    /// <paramref name="message" />
    /// when the precondition has not been met
    /// </summary>
    /// <typeparam name="T">Current type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">Name of the parameter.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>
    /// The value itself.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    /// Throws a <see cref="ArgumentNullException" /> when <paramref name="value" /> is a null reference.
    /// </remarks>
    [ContractAnnotation("value: null => stop")]
    [return: SysNotNull]
    public static T Guard<T>([SysNotNull] [CanBeNull] this T value, string? paramName) =>
        value.Guard(paramName, null);

    [ContractAnnotation("value: null => stop")]
    [return: SysNotNull]
    public static T Guard<T>([SysNotNull] [CanBeNull] this T value, string? paramName, string? message)
    {
        if (value is null)
            throw new ArgumentNullException(paramName, message ?? "Precondition not met.");

        return value;
    }

    /// <summary>
    /// Guards the specified <paramref name="predicate" /> from being violated by
    /// throwing an exception of type <see cref="ArgumentNullException" /> with a specific
    /// <paramref name="message" />
    /// when the precondition has not been met
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="value">Value to check.</param>
    /// <param name="predicate">The precondition that has to be met</param>
    /// <param name="paramName">Name of the parameter.</param>
    /// <param name="message">The message that will be included in the exception</param>
    /// <returns>
    /// The value itself.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    /// Throws a <see cref="ArgumentNullException" /> if the condition is false.
    /// </remarks>
    [ContractAnnotation("value: null => stop")]
    public static T Guard<T>([CanBeNull] this T value, Func<T, bool> predicate, string? paramName) =>
        value.Guard(predicate, paramName, null);

    [ContractAnnotation("value: null => stop")]
    public static T Guard<T>([CanBeNull] this T value, Func<T, bool> predicate, string? paramName, string? message)
    {
#if NET9_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(predicate);
#else
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));
#endif
        if (!predicate(value))
            throw new ArgumentNullException(paramName, message ?? "Precondition not met.");

        return value;
    }
}