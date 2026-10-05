using System;

namespace FEx.Sqlx;

internal static class SmoEnumMapper
{
    /// <summary>Maps an SMO enum value to the abstraction enum member with the same numeric value.</summary>
    public static TTo MapByValue<TTo>(this Enum smoValue, TTo fallback) where TTo : struct, Enum =>
        Convert.ToInt32(smoValue).MapByValue(fallback);

    /// <summary>Maps a raw numeric value to the abstraction enum member with that value, or <paramref name="fallback"/> when undefined.</summary>
    public static TTo MapByValue<TTo>(this int value, TTo fallback) where TTo : struct, Enum
    {
        var mapped = (TTo)Enum.ToObject(typeof(TTo), value);
        return Enum.IsDefined(typeof(TTo), mapped) ? mapped : fallback;
    }
}
