using System.Runtime.InteropServices;

namespace FEx.WPFx.Natives;

[StructLayout(LayoutKind.Sequential)]
public struct Minmaxinfo
{
    public ScreenPoint ptReserved;
    public ScreenPoint ptMaxSize;
    public ScreenPoint ptMaxPosition;
    public ScreenPoint ptMinTrackSize;
    public ScreenPoint ptMaxTrackSize;
}