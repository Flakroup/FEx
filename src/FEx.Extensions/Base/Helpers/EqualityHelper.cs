using System.Collections.Generic;

namespace FEx.Extensions.Base.Helpers;

public static class EqualityHelper
{
    public static bool IsEqual<T>(ref T field, T value) => EqualityComparer<T>.Default.Equals(field, value);

    public static bool IsNotEqual<T>(ref T field, T value) => !EqualityComparer<T>.Default.Equals(field, value);

    public static bool IsEqual<T>(T field, T value) => EqualityComparer<T>.Default.Equals(field, value);

    public static bool IsNotEqual<T>(T field, T value) => !EqualityComparer<T>.Default.Equals(field, value);

    public static bool SetFieldIfChanged<T>(ref T backingField, T newValue)
    {
        if (IsEqual(ref backingField, newValue))
            return false;

        backingField = newValue;
        return true;
    }
}