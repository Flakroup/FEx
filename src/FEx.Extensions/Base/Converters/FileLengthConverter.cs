using FEx.Extensions.Base.Enums;
using System;
using System.IO;

namespace FEx.Extensions.Base.Converters;

public static class FileLengthConverter
{
    /// <summary>
    ///     Converts the length of the file.
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
    ///     Converts the length of the file.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <param name="input">The input.</param>
    /// <param name="output">The output.</param>
    /// <param name="digits">Number of fractional digits in the return value</param>
    /// <returns></returns>
    public static double ConvertFileLength(long size, LengthType input, LengthType output, int digits = 3) =>
        ConvertFileLength(Convert.ToDouble(size), input, output, digits).length;

    /// <summary>
    ///     Converts the length of the file.
    /// </summary>
    /// <param name="fi">The fi.</param>
    /// <param name="output">The output.</param>
    /// <param name="digits">Number of fractional digits in the return value</param>
    /// <returns></returns>
    public static double ConvertFileLength(FileInfo fi, LengthType output, int digits = 3) =>
        ConvertFileLength(fi.Length, LengthType.Bytes, output, digits);

    public static string ConvertFileLengthToString(double size, LengthType input, LengthType output, int digits = 3)
    {
        if (output == LengthType.AutoDetect)
            output = GetOutputLenghtType(size);

        double roundedLength = ConvertFileLength(size, input, output, digits).length;
        string lenghtString = digits > 0
            ? string.Format($"{{0:0.{new string('0', digits)}}}", roundedLength)
            : roundedLength.ToString();

        return $"{lenghtString} {GetUnitShortcut(output)}";
    }

    public static LengthType GetOutputLenghtType(long size) => GetOutputLenghtType(Convert.ToDouble(size));

    public static LengthType GetOutputLenghtType(double size)
    {
        double pow = Math.Log10(size);
        return pow >= 12 ? LengthType.Terabytes :
            pow >= 9 ? LengthType.Gigabytes :
            pow >= 6 ? LengthType.Megabytes :
            pow >= 3 ? LengthType.Kilobytes : LengthType.Bytes;
    }

    public static double GetLength(LengthType lengthType) => Math.Pow(1024, (double)lengthType);

    private static string GetUnitShortcut(LengthType lengthType, double size = 0)
    {
        return lengthType switch
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
}