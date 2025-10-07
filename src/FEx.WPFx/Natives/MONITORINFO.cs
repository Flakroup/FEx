using System.Runtime.InteropServices;

namespace FEx.WPFx.Natives;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public class Monitorinfo
{
    /// <summary>
    /// The cb size
    /// </summary>
    public int CbSize { get; set; } = Marshal.SizeOf(typeof(Monitorinfo));

    /// <summary>
    /// The rc monitor
    /// </summary>
    public RectStruct RcMonitor { get; set; } = new();

    /// <summary>
    /// The rc work
    /// </summary>
    public RectStruct RcWork { get; set; } = new();

    /// <summary>
    /// The dw flags
    /// </summary>
    public int DwFlags { get; set; } = 0;
}