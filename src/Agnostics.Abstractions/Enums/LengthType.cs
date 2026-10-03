namespace FEx.Agnostics.Abstractions.Enums;

/// <summary>Unit used to express a length in bytes.</summary>
public enum LengthType
{
    /// <summary>Pick the largest unit that fits the value.</summary>
    AutoDetect = -1,
    /// <summary>Bytes.</summary>
    Bytes = 0,
    /// <summary>Kilobytes (1024 bytes).</summary>
    Kilobytes = 1,
    /// <summary>Megabytes (1024 kilobytes).</summary>
    Megabytes = 2,
    /// <summary>Gigabytes (1024 megabytes).</summary>
    Gigabytes = 3,
    /// <summary>Terabytes (1024 gigabytes).</summary>
    Terabytes = 4
}