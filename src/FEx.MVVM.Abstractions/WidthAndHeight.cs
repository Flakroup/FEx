using System;

namespace FEx.MVVM.Abstractions;

public class WidthAndHeight : IComparable<WidthAndHeight>, IEquatable<WidthAndHeight>
{
    public static WidthAndHeight Default { get; } = new(-1, -1);

    public int Width { get; }
    public int Height { get; }

    public WidthAndHeight(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int CompareTo(WidthAndHeight other)
    {
        if (Width > other?.Width
            || Height > other?.Height)
            return -1;

        if (Width == other?.Width
            && Height == other.Height)
            return 0;

        return 1;
    }

    public bool Equals(WidthAndHeight other)
    {
        return Width == other?.Width && Height == other.Height;
    }
}