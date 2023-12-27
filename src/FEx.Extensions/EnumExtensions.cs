using FEx.Extensions.Collections.Enumerables;
using System;
using System.Collections.Generic;
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
    /// <returns>Attributes collection.</returns>
    public static IReadOnlyCollection<TAttributeType> GetEnumValueAttributes<TAttributeType>(this Enum enumValue)
        where TAttributeType : Attribute =>
        (enumValue.GetType()
             .GetField(enumValue.ToString())
             ?.GetCustomAttributes(typeof(TAttributeType), true)
             .Cast<TAttributeType>()
         ?? Enumerable.Empty<TAttributeType>()).ToList()
        .AsReadOnly();

    public static TEnum? TryParse<TEnum>(this string value, bool ignoreCase = false) where TEnum : struct =>
        Enum.TryParse(value, ignoreCase, out TEnum result)
            ? result
            : null;

    public static IReadOnlyCollection<TEnum> GetEnumValues<TEnum>(this TEnum value) where TEnum : struct =>
        Enum.GetValues(value.GetType()).Cast<TEnum>().Distinct().ToList().AsReadOnly();

    public static IReadOnlyCollection<TEnum> GetEnumValues<TEnum>() where TEnum : struct =>
        Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Distinct().ToList().AsReadOnly();
}