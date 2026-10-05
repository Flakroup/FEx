using System;

namespace FEx.Json.Abstractions;

/// <summary>Argument checks shared by the <see cref="IFExJsonSerializer" /> implementations, so both reject alike.</summary>
public static class FExJsonGuard
{
    /// <summary>Throws <see cref="ArgumentException" /> when <paramref name="value" /> is not an <paramref name="inputType" />.</summary>
    public static void EnsureAssignable(object? value, Type inputType)
    {
        if (inputType is null)
            throw new ArgumentNullException(nameof(inputType));

        if (value is not null
            && !inputType.IsInstanceOfType(value))
            throw new ArgumentException(
                $"The value of type {value.GetType().FullName} is not an instance of {inputType.FullName}.",
                nameof(value));
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
