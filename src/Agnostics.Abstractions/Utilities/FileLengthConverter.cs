using FEx.Agnostics.Abstractions.Enums;
using System;
using System.IO;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>Converts file sizes between byte-based units (powers of 1024) and formats them as text.</summary>
public static class FileLengthConverter
{
    /// <summary>
    /// Converts the length of the file.
    /// </summary>
    /// <param name="length">The length.</param>
    /// <param name="input">The input.</param>
    /// <param name="output">The output.</param>
    /// <param name="digits">Number of fractional digits in the return value</param>
    /// <returns></returns>
    public static (double length, LengthType output) ConvertFileLength(double length,
                                                                       LengthType input,
                                                                       LengthType output,
                                                                       int digits = 3)
    {
        if (output == LengthType.AutoDetect)
            output = GetOutputLenghtType(length);

        if (input != output)
        {
            length *= Math.Pow(1024, (int)input);
            length /= Math.Pow(1024, (int)output);
        }

        return (Math.Round(length, digits, MidpointRounding.AwayFromZero), output);
    }

    /// <summary>
    /// Converts the length of the file.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <param name="input">The input.</param>
    /// <param name="output">The output.</param>
    /// <param name="digits">Number of fractional digits in the return value</param>
    /// <returns></returns>
    public static double ConvertFileLength(long size, LengthType input, LengthType output, int digits = 3) =>
        ConvertFileLength(Convert.ToDouble(size), input, output, digits).length;

    /// <summary>
    /// Converts the length of the file.
    /// </summary>
    /// <param name="fi">The fi.</param>
    /// <param name="output">The output.</param>
    /// <param name="digits">Number of fractional digits in the return value</param>
    /// <returns></returns>
    public static double ConvertFileLength(FileInfo fi, LengthType output, int digits = 3) =>
        ConvertFileLength(fi.Length, LengthType.Bytes, output, digits);

    /// <summary>Converts a size to a formatted string with a unit suffix.</summary>
    /// <param name="size">The size, expressed in <paramref name="input" /> units.</param>
    /// <param name="input">The unit of <paramref name="size" />.</param>
    /// <param name="output">The unit to convert to; <see cref="LengthType.AutoDetect" /> picks the best fitting one.</param>
    /// <param name="digits">The number of fractional digits.</param>
    /// <returns>The formatted size, for example <c>1.500 MB</c>.</returns>
    public static string ConvertFileLengthToString(double size, LengthType input, LengthType output, int digits = 3)
    {
        if (output == LengthType.AutoDetect)
            output = GetOutputLenghtType(size);

        var roundedLength = ConvertFileLength(size, input, output, digits).length;

        var lenghtString = digits > 0
            ? string.Format($"{{0:0.{new string('0', digits)}}}", roundedLength)
            : roundedLength.ToString();

        return $"{lenghtString} {GetUnitShortcut(output)}";
    }

    /// <summary>Picks the unit that best fits a size in bytes.</summary>
    /// <param name="size">The size in bytes.</param>
    /// <returns>The largest unit for which the size is at least one thousand of the previous unit.</returns>
    public static LengthType GetOutputLenghtType(long size) => GetOutputLenghtType(Convert.ToDouble(size));

    /// <summary>Picks the unit that best fits a size in bytes.</summary>
    /// <param name="size">The size in bytes.</param>
    /// <returns>The largest unit for which the size is at least one thousand of the previous unit.</returns>
    public static LengthType GetOutputLenghtType(double size)
    {
        var pow = Math.Log10(size);

        return pow >= 12 ? LengthType.Terabytes :
            pow >= 9 ? LengthType.Gigabytes :
            pow >= 6 ? LengthType.Megabytes :
            pow >= 3 ? LengthType.Kilobytes : LengthType.Bytes;
    }

    /// <summary>Gets the number of bytes in one unit.</summary>
    /// <param name="lengthType">The unit.</param>
    /// <returns>1024 raised to the power of the unit.</returns>
    public static double GetLength(LengthType lengthType) => Math.Pow(1024, (double)lengthType);

    /// <summary>Parses a unit abbreviation.</summary>
    /// <param name="unitShortcut">One of <c>B</c>, <c>KB</c>, <c>MB</c>, <c>GB</c> or <c>TB</c>.</param>
    /// <returns>The matching unit, or <see cref="LengthType.AutoDetect" /> when unrecognized.</returns>
    public static LengthType GetLengthType(string unitShortcut) =>
        unitShortcut switch
        {
            "B" => LengthType.Bytes,
            "KB" => LengthType.Kilobytes,
            "MB" => LengthType.Megabytes,
            "GB" => LengthType.Gigabytes,
            "TB" => LengthType.Terabytes,
            _ => LengthType.AutoDetect
        };

    private static string? GetUnitShortcut(LengthType lengthType, double size = 0) =>
        lengthType switch
        {
            LengthType.Bytes => "B",
            LengthType.Kilobytes => "KB",
            LengthType.Megabytes => "MB",
            LengthType.Gigabytes => "GB",
            LengthType.Terabytes => "TB",
            LengthType.AutoDetect => GetUnitShortcut(GetOutputLenghtType(size)),
            _ => null
        };
}