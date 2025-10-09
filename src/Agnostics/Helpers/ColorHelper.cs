using System.Drawing;
using System.Globalization;

namespace FEx.Agnostics.Helpers;

public static class ColorHelper
{
    /// <summary>
    ///     Gets <see cref="Color" /> from ARGB string.
    /// </summary>
    /// <param name="colorcode">The colorcode.</param>
    /// <returns>
    ///     <see cref="Color" />
    /// </returns>
    public static Color FromArgbString(string colorcode)
    {
        colorcode = colorcode.Replace("#", "");
        var a = byte.Parse(colorcode.Substring(0, 2), NumberStyles.HexNumber);
        var r = byte.Parse(colorcode.Substring(2, 2), NumberStyles.HexNumber);
        var g = byte.Parse(colorcode.Substring(4, 2), NumberStyles.HexNumber);
        var b = byte.Parse(colorcode.Substring(6, 2), NumberStyles.HexNumber);

        return Color.FromArgb(a, r, g, b);
    }

    /// <summary>
    ///     Froms the RGB string.
    /// </summary>
    /// <param name="colorcode">The colorcode.</param>
    /// <returns></returns>
    public static Color FromRgbString(string colorcode)
    {
        colorcode = colorcode.Replace("#", "");
        var r = byte.Parse(colorcode.Substring(0, 2), NumberStyles.HexNumber);
        var g = byte.Parse(colorcode.Substring(2, 2), NumberStyles.HexNumber);
        var b = byte.Parse(colorcode.Substring(4, 2), NumberStyles.HexNumber);

        return Color.FromArgb(255, r, g, b);
    }
}