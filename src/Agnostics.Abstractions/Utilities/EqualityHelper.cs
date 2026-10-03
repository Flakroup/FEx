using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>Equality helpers based on the default equality comparer.</summary>
public static class EqualityHelper
{
    /// <summary>Determines whether a field equals a value</summary>
    /// <typeparam name="T">The compared type.</typeparam>
    /// <param name="field">The field to compare.</param>
    /// <param name="value">The value to compare with.</param>
    /// <returns><c>true</c> if they are equal.</returns>
    public static bool IsEqual<T>(ref T field, T value) => EqualityComparer<T>.Default.Equals(field, value);

    /// <summary>Determines whether a field differs from a value</summary>
    /// <typeparam name="T">The compared type.</typeparam>
    /// <param name="field">The field to compare.</param>
    /// <param name="value">The value to compare with.</param>
    /// <returns><c>true</c> if they differ.</returns>
    public static bool IsNotEqual<T>(ref T field, T value) => !EqualityComparer<T>.Default.Equals(field, value);

    /// <summary>Determines whether two values are equal</summary>
    /// <typeparam name="T">The compared type.</typeparam>
    /// <param name="field">The first value.</param>
    /// <param name="value">The second value.</param>
    /// <returns><c>true</c> if they are equal.</returns>
    public static bool IsEqual<T>(T field, T value) => EqualityComparer<T>.Default.Equals(field, value);

    /// <summary>Determines whether two values differ</summary>
    /// <typeparam name="T">The compared type.</typeparam>
    /// <param name="field">The first value.</param>
    /// <param name="value">The second value.</param>
    /// <returns><c>true</c> if they differ.</returns>
    public static bool IsNotEqual<T>(T field, T value) => !EqualityComparer<T>.Default.Equals(field, value);

    /// <summary>Assigns a new value to a backing field only when it differs from the current one</summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="backingField">The field to update.</param>
    /// <param name="newValue">The new value.</param>
    /// <returns><c>true</c> if the field was changed.</returns>
    public static bool SetFieldIfChanged<T>(ref T backingField, T newValue)
    {
        if (IsEqual(ref backingField, newValue))
            return false;

        backingField = newValue;

        return true;
    }
}