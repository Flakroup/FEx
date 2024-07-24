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

    public static IReadOnlyCollection<TEnum> GetEnumValues<TEnum>(this TEnum value) where TEnum : struct =>
        Enum.GetValues(value.GetType()).Cast<TEnum>().Distinct().ToList().AsReadOnly();

    public static IReadOnlyCollection<TEnum> GetEnumValues<TEnum>() where TEnum : struct =>
        Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Distinct().ToList().AsReadOnly();
}