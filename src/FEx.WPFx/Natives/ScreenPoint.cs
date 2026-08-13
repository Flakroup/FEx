using System.Runtime.InteropServices;

namespace FEx.WPFx.Natives;

/// <summary>
/// POINT aka POINTAPI
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ScreenPoint
{
    /// <summary>
    /// x coordinate of point.
    /// </summary>
    public int X;

    /// <summary>
    /// y coordinate of point.
    /// </summary>
    public int Y;

    /// <summary>
    /// Construct a point of coordinates (x,y).
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public ScreenPoint(int x, int y)
    {
        X = x;
        Y = y;
    }
}