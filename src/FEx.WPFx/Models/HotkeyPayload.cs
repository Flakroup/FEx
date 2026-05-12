using FEx.MVVM.Rx.Legacy.BaseObjects;
using System.Windows.Input;

namespace FEx.WPFx.Models;

public class HotkeyPayload
{
    public KeyGesture KeyGesture { get; }

    public ViewModelBase Origin { get; }

    public HotkeyPayload(KeyGesture keyGesture, ViewModelBase origin)
    {
        Origin = origin;
        KeyGesture = keyGesture;
    }
}
