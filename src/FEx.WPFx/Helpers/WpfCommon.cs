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

            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame),
                frame);

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
