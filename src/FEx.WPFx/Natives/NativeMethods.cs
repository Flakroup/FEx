using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FEx.WPFx.Natives;

/// <summary>
/// Contains the external references to the unmanaged code.
/// </summary>
public static class NativeMethods
{
    /// <summary>
    /// Brings window to the foreground.
    /// </summary>
    /// <param name="window">The window.</param>
    public static void BringOnTop(this Window window)
    {
        try
        {
            var retries = 3;
            var result = false;
            IntPtr windowHandle = GetWindowHandle(window);

            while (!result
                   && retries > 0)
            {
                window.Dispatcher.Invoke(() =>
                {
                    result = SetForegroundWindow(windowHandle) != 0;
                    retries--;
                });
            }
        }
        catch
        {
            //ignored
        }
    }

    /// <summary>
    /// Gets the last error.
    /// </summary>
    /// <returns></returns>
    public static Win32Exception GetLastError() => new(Marshal.GetLastWin32Error());

    /// <summary>
    /// Gets the root windows of process.
    /// </summary>
    /// <param name="process">The process.</param>
    /// <returns>
    /// List{Window}
    /// </returns>
    public static List<KeyValuePair<uint, Window>> GetRootWindowsOfProcess(Process process = null)
    {
        process ??= Process.GetCurrentProcess();

        IEnumerable<IntPtr> rootWindows = GetChildWindows(IntPtr.Zero);
        var dsProcRootWindows = new List<KeyValuePair<uint, Window>>();

        foreach (IntPtr hWnd in rootWindows)
        {
            uint threadId = GetWindowThreadProcessId(hWnd, out uint lpdwProcessId);

            if (lpdwProcessId == process.Id)
            {
                using var hwndSource = HwndSource.FromHwnd(hWnd);

                if (hwndSource?.RootVisual is Window wnd)
                    dsProcRootWindows.Add(new(threadId, wnd));
            }
        }

        return dsProcRootWindows;
    }

    public static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
    {
        var mmi = (Minmaxinfo)Marshal.PtrToStructure(lParam, typeof(Minmaxinfo));

        // Adjust the maximized size and position to fit the work area of the correct monitor
        const int monitorDefaulttonearest = 0x00000002;
        IntPtr monitor = MonitorFromWindow(hwnd, monitorDefaulttonearest);

        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new Monitorinfo();
            GetMonitorInfo(monitor, monitorInfo);
            RectStruct rcWorkArea = monitorInfo.RcWork;
            RectStruct rcMonitorArea = monitorInfo.RcMonitor;
            mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
            mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
            mmi.ptMaxSize.X = Math.Abs(rcWorkArea.right - rcWorkArea.left);
            mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
        }

        Marshal.StructureToPtr(mmi, lParam, true);
    }

    public static void MaximizeWindow(this Process proc) => MaximizeWindow(proc.MainWindowHandle);

    public static void MaximizeWindow(this IntPtr hwnd) => ShowWindow(hwnd, (int)Natives.ShowWindow.SW_MAXIMIZE);

    public static void MinimizeWindow(this Process proc) => MinimizeWindow(proc.MainWindowHandle);

    public static void MinimizeWindow(this IntPtr hwnd) => ShowWindow(hwnd, (int)Natives.ShowWindow.SW_MINIMIZE);

    /// <summary>
    /// Retrieves information about the specified window. The function also retrieves the 32-bit (DWORD) value at the
    /// specified offset into the extra window memory.
    /// </summary>
    /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
    /// <param name="nIndex">
    /// The zero-based offset to the value to be retrieved. Valid values are in the range zero through the
    /// number of bytes of extra window memory, minus four; for example, if you specified 12 or more bytes of extra memory,
    /// a value of 8 would be an index to the third 32-bit integer. To retrieve any other value, specify one of the
    /// following values.
    /// </param>
    /// <returns>
    /// System.Int32 If the function succeeds, the return value is the requested value.
    /// If the function fails, the return value is zero.To get extended error information, call GetLastError.
    /// If SetWindowLong has not been called previously, GetWindowLong returns zero for values in the extra window or class
    /// memory.
    /// </returns>
    /// <remarks>
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms633584(v=vs.85).aspx
    /// </remarks>
    [DllImport("user32.dll", SetLastError = true)]
    // ReSharper disable UnusedMember.Local
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    // ReSharper restore UnusedMember.Local

    /// <summary>
    /// Changes an attribute of the specified window. The function also sets the 32-bit (long) value at the specified
    /// offset into the extra window memory.
    /// </summary>
    /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs..</param>
    /// <param name="nIndex">
    /// The zero-based offset to the value to be set. Valid values are in the range zero through the
    /// number of bytes of extra window memory, minus the size of an integer. To set any other value, specify one of the
    /// following values: GWL_EXSTYLE, GWL_HINSTANCE, GWL_ID, GWL_STYLE, GWL_USERDATA, GWL_WNDPROC
    /// </param>
    /// <param name="dwNewLong">The replacement value.</param>
    /// <returns>
    /// If the function succeeds, the return value is the previous value of the specified 32-bit integer.
    /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
    /// </returns>
    /// <remarks>
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms633591(v=vs.85).aspx
    /// </remarks>
    [DllImport("user32.dll", SetLastError = true)]
    // ReSharper disable UnusedMember.Local
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    // ReSharper restore UnusedMember.Local

    /// <summary>
    /// Brings the thread that created the specified window into the foreground and activates the window. Keyboard input is
    /// directed to the window, and various visual cues are changed for the user. The system assigns a slightly higher
    /// priority to the thread that created the foreground window than it does to other threads.
    /// </summary>
    /// <param name="hWnd">
    /// C++ ( hWnd [in]. Type: HWND )<br />A handle to the window that should be activated and brought to
    /// the foreground.
    /// </param>
    /// <returns>
    /// <c>true</c> or nonzero if the window was brought to the foreground, <c>false</c> or zero If the window was not
    /// brought to the foreground.
    /// </returns>
    /// <remarks>
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539%28v=vs.85%29.aspx
    /// </remarks>
    [DllImport("User32.dll", SetLastError = true)]
    private static extern int SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the
    /// process that created the window.
    /// </summary>
    /// <param name="hWnd">A handle to the window. </param>
    /// <param name="lpdwProcessId">
    /// A pointer to a variable that receives the process identifier. If this parameter is not
    /// NULL, GetWindowThreadProcessId copies the identifier of the process to the variable; otherwise, it does not.
    /// </param>
    /// <returns>The return value is the identifier of the thread that created the window. </returns>
    /// <remarks>http://msdn.microsoft.com/en-us/library/ms633522%28v=vs.85%29.aspx</remarks>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Enumerates the child windows that belong to the specified parent window by passing the handle to each child
    /// window, in turn, to an application-defined callback function. EnumChildWindows continues until the last child
    /// window is enumerated or the callback function returns FALSE.
    /// </summary>
    /// <param name="parentHandle">
    /// A handle to the parent window whose child windows are to be enumerated. If this parameter is
    /// NULL, this function is equivalent to EnumWindows.
    /// </param>
    /// <param name="callback">A pointer to an application-defined callback function. For more information, see EnumChildProc..</param>
    /// <param name="lParam">An application-defined value to be passed to the callback function.</param>
    /// <returns>
    /// System.Boolean The return value is not used.
    /// </returns>
    /// <remarks>
    /// If a child window has created child windows of its own, EnumChildWindows enumerates those windows as well.
    /// A child window that is moved or repositioned in the Z order during the enumeration process will be properly
    /// enumerated.The function does not enumerate a child window that is destroyed before being enumerated or
    /// that is created during the enumeration process.
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms633494(v=vs.85).aspx
    /// </remarks>
    [DllImport("user32.Dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

    [DllImport("user32")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, Monitorinfo lpmi);

    [DllImport("User32")]
    private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static IntPtr GetWindowHandle(Window window) =>
        window.Dispatcher.Invoke(() => new WindowInteropHelper(window).Handle);

    /// <summary>
    /// Gets the child windows.
    /// </summary>
    /// <param name="parent">The parent.</param>
    /// <returns>
    /// IEnumerable{IntPtr}
    /// </returns>
    private static IEnumerable<IntPtr> GetChildWindows(IntPtr parent)
    {
        var result = new List<IntPtr>();
        var listHandle = GCHandle.Alloc(result);

        try
        {
            Win32Callback childProc = EnumWindow;
            EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
        }
        finally
        {
            if (listHandle.IsAllocated)
                listHandle.Free();
        }

        return result;
    }

    /// <summary>
    /// Enums the window.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <param name="pointer">The pointer.</param>
    /// <returns>
    /// System.Boolean
    /// </returns>
    /// <exception cref="InvalidCastException">GCHandle Target could not be cast as List{IntPtr}</exception>
    private static bool EnumWindow(IntPtr handle, IntPtr pointer)
    {
        var gch = GCHandle.FromIntPtr(pointer);

        if (gch.Target is List<IntPtr> list)
        {
            list.Add(handle);

            return true;
        }

        return false; //GCHandle Target could not be cast as List<IntPtr>
    }

    /// <summary>
    /// Callback of Win32
    /// </summary>
    /// <param name="hwnd">The HWND.</param>
    /// <param name="lParam">The l parameter.</param>
    /// <returns></returns>
    private delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);
}