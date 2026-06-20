using FEx.Agnostics.Abstractions.Extensions;
using FEx.WPFx.Attributes;
using System;

namespace FEx.WPFx.Extensions;

/// <summary>
///     Icon extensions class.
/// </summary>
public static class IconExtensions
{
    /// <summary>
    ///     Gets the icon.
    /// </summary>
    /// <typeparam name="TIcon">The type of the T icon.</typeparam>
    /// <param name="iconCharacter">The icon character.</param>
    /// <returns></returns>
    public static TIcon GetIcon<TIcon>(this char iconCharacter)
    {
        foreach (TIcon icon in Enum.GetValues(typeof(TIcon)))
        {
            if (GetIconCharacter(icon as Enum) == iconCharacter)
                return icon;
        }

        throw new ArgumentException("Invalid character: " + iconCharacter);
    }

    /// <summary>
    ///     Gets the icon character.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Icon character.</returns>
    public static char GetIconCharacter(this Enum value) => GetIconPropertyValue(value, x => x.Character, x => x.Character);

    /// <summary>
    ///     Gets the alt text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Alt text.</returns>
    public static string GetAltText(this Enum value) => GetIconPropertyValue(value,
            x => x.AltText,
            x => !string.IsNullOrWhiteSpace(x.AltText)
                ? x.AltText
                : value.ToString());

    /// <summary>
    ///     Gets the icon property value.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="iconPropertySelector">The icon property selector.</param>
    /// <param name="iconDescriptorPropertySelector">The icon descriptor property selector.</param>
    /// <param name="defaultValueSelector">The default value selector.</param>
    /// <returns>Icon property value.</returns>
    private static TResult GetIconPropertyValue<TResult>(Enum value,
                                                         Func<IconAttribute, TResult> iconPropertySelector,
                                                         Func<IconDescriptorAttribute, TResult>
                                                             iconDescriptorPropertySelector,
                                                         Func<Enum, TResult> defaultValueSelector = null)
    {
        if (value is not null)
        {
            var iconAttribute = value.GetEnumValueAttribute<IconAttribute>();

            if (iconAttribute is not null)
                return iconPropertySelector(iconAttribute);

            var iconDescriptorAttribute = value.GetEnumValueAttribute<IconDescriptorAttribute>();

            if (iconDescriptorAttribute is not null)
                return iconDescriptorPropertySelector(iconDescriptorAttribute);
        }

        return defaultValueSelector is not null
            ? defaultValueSelector(value)
            : default;
    }
}
