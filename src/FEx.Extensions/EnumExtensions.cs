using FEx.Extensions.Collections.Enumerables;
using System;
using System.ComponentModel;
using System.Linq;

namespace FEx.Extensions;

public static class EnumExtensions
{
    /// <summary>
    ///     Gets the enum value description.
    /// </summary>
    /// <param name="enumValue">The enum value.</param>
    /// <returns></returns>
    public static string GetEnumValueDescription(this Enum enumValue) =>
        enumValue.GetEnumValueAttribute<DescriptionAttribute>()?.Description;

    /// <summary>
    ///     Generic method getting attribute object of the given type from enumerated value.
    /// </summary>
    /// <typeparam name="TAttributeType">Attribute type.</typeparam>
    /// <param name="enumValue">Enumerator type.</param>
    /// <returns>Attribute object.</returns>
    public static TAttributeType GetEnumValueAttribute<TAttributeType>(this Enum enumValue)
        where TAttributeType : Attribute =>
        GetEnumValueAttributes<TAttributeType>(enumValue).FindInEnumerable();

    /// <summary>
    ///     Generic method getting attribute objects of the given type from enumerated value.
    /// </summary>
    /// <typeparam name="TAttributeType">Attribute type.</typeparam>
    /// <param name="enumValue">Enumerator value.</param>
    /// <returns>Attribute object.</returns>
    public static TAttributeType[] GetEnumValueAttributes<TAttributeType>(this Enum enumValue)
        where TAttributeType : Attribute =>
        Enum.IsDefined(enumValue.GetType(), enumValue)
            ? (TAttributeType[])enumValue.GetType()
                .GetField(enumValue.ToString())
                .GetCustomAttributes(typeof(TAttributeType), true)
            : null;

    public static TEnum? TryParse<TEnum>(this string value, bool ignoreCase = false) where TEnum : struct
    {
        bool isSuccess = Enum.TryParse(value, ignoreCase, out TEnum result);

        return isSuccess
            ? result
            : null;
    }

    public static TEnum[] GetEnumValues<TEnum>(this TEnum value) where TEnum : struct =>
        Enum.GetValues(value.GetType()).Cast<TEnum>().ToArray();

    public static TEnum[] GetEnumValues<TEnum>() where TEnum : struct =>
        Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToArray();
}