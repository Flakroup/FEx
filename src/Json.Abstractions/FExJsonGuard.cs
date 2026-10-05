using System;
using System.IO;

namespace FEx.Json.Abstractions;

/// <summary>Argument checks shared by the <see cref="IFExJsonSerializer" /> implementations, so both reject alike.</summary>
public static class FExJsonGuard
{
    /// <summary>
    /// Throws <see cref="ArgumentException" /> when <paramref name="value" /> is not an <paramref name="inputType" />,
    /// including a null value for a non-nullable value type.
    /// </summary>
    public static void EnsureAssignable(object? value, Type inputType)
    {
        if (inputType is null)
            throw new ArgumentNullException(nameof(inputType));

        if (value is null)
        {
            // Newtonsoft writes null for any type while System.Text.Json rejects it for a value type: reject it in both.
            if (inputType.IsValueType
                && Nullable.GetUnderlyingType(inputType) is null)
                throw new ArgumentException($"A null value is not an instance of {inputType.FullName}.", nameof(value));

            return;
        }

        if (!inputType.IsInstanceOfType(value))
            throw new ArgumentException(
                $"The value of type {value.GetType().FullName} is not an instance of {inputType.FullName}.",
                nameof(value));
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException" /> for a null stream and <see cref="ArgumentException" /> for one that
    /// cannot be read, before the library touches it, so a caller's stream misuse is never reported as a JSON failure.
    /// </summary>
    public static void EnsureReadable(Stream utf8Json)
    {
        if (utf8Json is null)
            throw new ArgumentNullException(nameof(utf8Json));

        if (!utf8Json.CanRead)
            throw new ArgumentException("The stream does not support reading.", nameof(utf8Json));
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException" /> for a null stream and <see cref="ArgumentException" /> for one that
    /// cannot be written, before the library touches it, so a caller's stream misuse is never reported as a JSON failure.
    /// </summary>
    public static void EnsureWritable(Stream utf8Json)
    {
        if (utf8Json is null)
            throw new ArgumentNullException(nameof(utf8Json));

        if (!utf8Json.CanWrite)
            throw new ArgumentException("The stream does not support writing.", nameof(utf8Json));
    }

    /// <summary>Throws <see cref="ArgumentNullException" /> for a null payload and <see cref="FExJsonException" /> for a blank one.</summary>
    public static void EnsurePayload(string json)
    {
        if (json is null)
            throw new ArgumentNullException(nameof(json));

        if (string.IsNullOrWhiteSpace(json))
            throw FExJsonException.EmptyPayload();
    }
}
