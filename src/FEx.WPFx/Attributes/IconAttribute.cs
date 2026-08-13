using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace FEx.WPFx.Attributes;

/// <summary>
///     Base icon attribute.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class IconAttribute : Attribute
{
    /// <summary>
    ///     Gets the icon.
    /// </summary>
    public Enum Icon { get; }

    /// <summary>
    ///     Gets the character.
    /// </summary>
    public char Character
    {
        get { return GetIconDescriptorAttributePropertyValue(x => x.Character); }
    }

    /// <summary>
    ///     Gets the alt text.
    /// </summary>
    public string? AltText
    {
        get { return GetIconDescriptorAttributePropertyValue(x => x.AltText); }
    }

    /// <summary>
    ///     Initializes a new instance of the IconAttribute class.
    /// </summary>
    /// <param name="icon">The icon.</param>
    public IconAttribute(Enum icon)
    {
        Icon = icon;
    }

    /// <summary>
    ///     Gets the icon descriptor attribute property.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <returns>Property value.</returns>
    [return: MaybeNull]
    private TResult GetIconDescriptorAttributePropertyValue<TResult>(
        Func<IconDescriptorAttribute, TResult> propertySelector)
    {
        var iconCharacterAttribute = Icon.GetEnumValueAttribute<IconDescriptorAttribute>();

        return iconCharacterAttribute is not null
            ? propertySelector(iconCharacterAttribute)
            : default;
    }
}
