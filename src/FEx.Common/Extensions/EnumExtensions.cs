using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace FEx.Common.Extensions;

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
         ?? []).ToList()
        .AsReadOnly();
}