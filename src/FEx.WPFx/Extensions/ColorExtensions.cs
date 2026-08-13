using System.Globalization;
using DColor = System.Drawing.Color;
using MColor = System.Windows.Media.Color;

namespace FEx.WPFx.Extensions;

public static class ColorExtensions
{
    public static MColor FromArgbString(string colorcode)
    {
        colorcode = colorcode.TrimStart('#');
        var a = byte.Parse(colorcode.Substring(0, 2), NumberStyles.HexNumber);
        var r = byte.Parse(colorcode.Substring(2, 2), NumberStyles.HexNumber);
        var g = byte.Parse(colorcode.Substring(4, 2), NumberStyles.HexNumber);
        var b = byte.Parse(colorcode.Substring(6, 2), NumberStyles.HexNumber);

        return MColor.FromArgb(a, r, g, b);
    }

    public static MColor FromRgbString(string colorcode)
    {
        colorcode = colorcode.TrimStart('#');
        var r = byte.Parse(colorcode.Substring(0, 2), NumberStyles.HexNumber);
        var g = byte.Parse(colorcode.Substring(2, 2), NumberStyles.HexNumber);
        var b = byte.Parse(colorcode.Substring(4, 2), NumberStyles.HexNumber);

        return MColor.FromRgb(r, g, b);
    }

    public static MColor ToMediaColor(this DColor color) => MColor.FromArgb(color.A, color.R, color.G, color.B);
}