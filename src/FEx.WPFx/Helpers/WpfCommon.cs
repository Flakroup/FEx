using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
#if ISNETFULL
using System.Security.Permissions;
#endif

namespace FEx.WPFx.Helpers;

public static class WpfCommon
{
    public static bool IsInDesignMode =>
        Application.Current is null
        || (bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue;

#if ISNETFULL
    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
#endif
    public static void DoEvents()
    {
        try
        {
            var frame = new DispatcherFrame();

            // VSTHRD001: WPF message-pump marshaling via Dispatcher.Invoke (DoEvents helper).
#pragma warning disable VSTHRD001
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame),
                frame);
#pragma warning restore VSTHRD001

            Dispatcher.PushFrame(frame);
        }
        catch
        {
            // ignored
        }
    }

    public static object ExitFrame(object frame)
    {
        ((DispatcherFrame)frame).Continue = false;

        return null;
    }
}