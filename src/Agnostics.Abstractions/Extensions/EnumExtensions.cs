using FEx.Common.Abstractions.Interfaces;
using FEx.Common.Collections;
using FEx.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Extensions;

public static class EnumExtensions
{
    public static TEnum? TryParse<TEnum>(this string value, bool ignoreCase = false) where TEnum : struct =>
        Enum.TryParse(value, ignoreCase, out TEnum result)
            ? result
            : null;

    public static IReadOnlyCollection<TEnum> GetEnumValues<TEnum>(this TEnum _) where TEnum : Enum =>
        GetEnumValues<TEnum>();

    public static IReadOnlyCollection<TEnum> GetEnumValues<TEnum>() where TEnum : Enum =>
        Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Distinct().ToList().AsReadOnly();

    public static IMap<string, TEnum> GetEnumMap<TEnum>(bool useValueDescription = false) where TEnum : Enum =>
        GetEnumCustomMap<TEnum, string, TEnum>(enumValue => useValueDescription
                ? enumValue.GetEnumValueDescription()
                : enumValue.ToString(),
            enumValue => enumValue);

    public static IMap<TKey, TValue>
        GetEnumCustomMap<TEnum, TKey, TValue>(Func<TEnum, TKey> keyGetter, Func<TEnum, TValue> valueGetter)
        where TEnum : Enum =>
        new Map<TKey, TValue>(GetEnumValues<TEnum>().ToDictionary(keyGetter, valueGetter), true);
}