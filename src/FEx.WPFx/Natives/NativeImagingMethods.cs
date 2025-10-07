using System;
using System.Runtime.InteropServices;

namespace FEx.WPFx.Natives;

/// <summary>
/// Contains the external references to the unmanaged code.
/// </summary>
public static class NativeImagingMethods
{
    public static void DeleteBitmapObject(IntPtr handle) => DeleteObject(handle);

    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject([In] IntPtr hObject);
}