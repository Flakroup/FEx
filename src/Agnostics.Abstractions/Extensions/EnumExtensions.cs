using FEx.Agnostics.Abstractions.Collections;
using FEx.Agnostics.Abstractions.Interfaces.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Gets the enum value description.
    /// </summary>
    /// <param name="enumValue">The enum value.</param>
    /// <returns>Description</returns>
    public static string? GetEnumValueDescription(this Enum enumValue) =>
        enumValue.GetEnumValueAttribute<DescriptionAttribute>()?.Description;

    /// <summary>
    /// Generic method getting attribute object of the given type from enumerated value.
    /// </summary>
    /// <typeparam name="TAttributeType">Attribute type.</typeparam>
    /// <param name="enumValue">Enumerator type.</param>
    /// <returns>Attribute object.</returns>
    public static TAttributeType? GetEnumValueAttribute<TAttributeType>(this Enum enumValue)
        where TAttributeType : Attribute =>
        enumValue.GetEnumValueAttributes<TAttributeType>()?.FirstOrDefault();

    /// <summary>
    /// Generic method getting attribute objects of the given type from enumerated value.
    /// </summary>
    /// <typeparam name="TAttributeType">Attribute type.</typeparam>
    /// <param name="enumValue">Enumerator value.</param>
    /// <returns>Attribute object.</returns>
    public static TAttributeType[]? GetEnumValueAttributes<TAttributeType>(this Enum enumValue)
        where TAttributeType : Attribute =>
        Enum.IsDefined(enumValue.GetType(), enumValue)
            // IsDefined guarantees the backing field exists for this value.
            ? (TAttributeType[])enumValue.GetType()
                .GetField(enumValue.ToString())!
                .GetCustomAttributes(typeof(TAttributeType), true)
            : null;

    public static TEnum? TryParse<TEnum>(this string value, bool ignoreCase = false) where TEnum : struct =>
        Enum.TryParse(value, ignoreCase, out TEnum result)
            ? result
            : null;

    public static IReadOnlyCollection<TEnum> GetEnumValues<TEnum>(this TEnum _) where TEnum : struct, Enum =>
        GetEnumValues<TEnum>();

    /// <summary>
    /// Gets the enum values.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enum.</typeparam>
    public static IReadOnlyCollection<TEnum> GetEnumValues<TEnum>() where TEnum : struct, Enum =>
#if NET9_0_OR_GREATER
        Enum.GetValuesAsUnderlyingType<TEnum>()
#else
        Enum.GetValues(typeof(TEnum))
#endif
            .Cast<TEnum>()
            .Distinct()
            .ToList()
            .AsReadOnly();

    public static IMap<string, TEnum> GetEnumMap<TEnum>(bool useValueDescription = false) where TEnum : struct, Enum =>
        GetEnumCustomMap<TEnum, string, TEnum>(enumValue => useValueDescription
                ? enumValue.GetEnumValueDescription()!
                : enumValue.ToString(),
            enumValue => enumValue);

    public static IMap<TKey, TValue>
        GetEnumCustomMap<TEnum, TKey, TValue>(Func<TEnum, TKey> keyGetter, Func<TEnum, TValue> valueGetter)
        where TEnum : struct, Enum
        where TKey : notnull
        where TValue : notnull =>
        new Map<TKey, TValue>(GetEnumValues<TEnum>().ToDictionary(keyGetter, valueGetter), true);
}