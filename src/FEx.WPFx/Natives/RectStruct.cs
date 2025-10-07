using System;
using System.Runtime.InteropServices;

namespace FEx.WPFx.Natives;

/// <summary> Win32 </summary>
[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct RectStruct
{
    /// <summary> Win32 </summary>
    public readonly int left;

    /// <summary> Win32 </summary>
    public readonly int top;

    /// <summary> Win32 </summary>
    public readonly int right;

    /// <summary> Win32 </summary>
    public readonly int bottom;

    /// <summary> Win32 </summary>
    // ReSharper disable once UnassignedReadonlyField
    public static readonly RectStruct Empty;

    /// <summary> Win32 </summary>
    public int Width => Math.Abs(right - left); // Abs needed for BIDI OS

    /// <summary> Win32 </summary>
    public int Height => bottom - top;

    /// <summary>
    /// Win32
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="top">The top.</param>
    /// <param name="right">The right.</param>
    /// <param name="bottom">The bottom.</param>
    public RectStruct(int left, int top, int right, int bottom)
    {
        this.left = left;
        this.top = top;
        this.right = right;
        this.bottom = bottom;
    }

    /// <summary>
    /// Win32
    /// </summary>
    /// <param name="rcSrc">The rc source.</param>
    public RectStruct(RectStruct rcSrc)
    {
        left = rcSrc.left;
        top = rcSrc.top;
        right = rcSrc.right;
        bottom = rcSrc.bottom;
    }

    /// <summary> Win32 </summary>
    public bool IsEmpty =>
        // BUGBUG : On Bidi OS (hebrew arabic) left > right
        left >= right || top >= bottom;

    /// <summary> Return a user friendly representation of this struct </summary>
    public override string ToString() =>
        this == Empty
            ? "RECT {Empty}"
            : "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";

    /// <summary>
    /// Determine if 2 RECT are equal (deep compare)
    /// </summary>
    /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object obj)
    {
        var r = obj as RectStruct?;

        return r.HasValue && this == r.Value;
    }

    /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
    public override int GetHashCode() =>
        left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();

    /// <summary>
    /// Determine if 2 RECT are equal (deep compare)
    /// </summary>
    /// <param name="rect1">The rect1.</param>
    /// <param name="rect2">The rect2.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(RectStruct rect1, RectStruct rect2) =>
        rect1.left == rect2.left
        && rect1.top == rect2.top
        && rect1.right == rect2.right
        && rect1.bottom == rect2.bottom;

    /// <summary>
    /// Determine if 2 RECT are different(deep compare)
    /// </summary>
    /// <param name="rect1">The rect1.</param>
    /// <param name="rect2">The rect2.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator !=(RectStruct rect1, RectStruct rect2) => !(rect1 == rect2);
}