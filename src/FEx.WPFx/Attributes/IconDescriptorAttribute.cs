using System;

namespace FEx.WPFx.Attributes;

/// <summary>
///     Icon character attribute.
/// </summary>
/// <seealso cref="System.Attribute" />
[AttributeUsage(AttributeTargets.All)]
public sealed class IconDescriptorAttribute : Attribute
{
    /// <summary>
    ///     Gets the value.
    /// </summary>
    public char Character { get; }

    /// <summary>
    ///     Gets the alt text.
    /// </summary>
    public string? AltText { get; }

    /// <summary>
    ///     Initializes a new instance of the IconCharacterAttribute class.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="altText">The alt text.</param>
    public IconDescriptorAttribute(char character, string? altText = null)
    {
        Character = character;
        AltText = altText;
    }
}
