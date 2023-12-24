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

        return Width == other?.Width && Height == other.Height
            ? 0
            : 1;
    }

    public bool Equals(WidthAndHeight other) =>
        other is not null && (ReferenceEquals(this, other) || Width == other.Width && Height == other.Height);

    public override bool Equals(object obj) =>
        obj is WidthAndHeight widthAndHeight
        && (ReferenceEquals(this, widthAndHeight) || widthAndHeight.GetType() == GetType() && Equals(widthAndHeight));

    public override int GetHashCode()
    {
#if NETSTANDARD
        unchecked
        {
            return Width * 397 ^ Height;
        }
#else
        return HashCode.Combine(Width, Height);
#endif
    }
}