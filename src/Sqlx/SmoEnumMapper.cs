using System;

namespace FEx.Sqlx;

internal static class SmoEnumMapper
{
    /// <summary>Maps an SMO enum value to the abstraction enum member with the same numeric value.</summary>
    public static TTo MapByValue<TTo>(this Enum smoValue, TTo fallback) where TTo : struct, Enum
    {
        var mapped = (TTo)Enum.ToObject(typeof(TTo), Convert.ToInt32(smoValue));
        return Enum.IsDefined(typeof(TTo), mapped) ? mapped : fallback;
    }
}
