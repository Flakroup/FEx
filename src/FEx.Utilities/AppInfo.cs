using System.Diagnostics;
using System.Reflection;

namespace FEx.Utilities;

public static class AppInfo
{
    static AppInfo()
    {
        EntryAssembly = Assembly.GetEntryAssembly();
        EntryAssemblyLocation = EntryAssembly?.Location != null ? new FileInfo(EntryAssembly?.Location) : null;
        EntryAssemblyName = EntryAssembly?.GetName().Name;
        ProductVersion = EntryAssemblyLocation != null
            ? FileVersionInfo.GetVersionInfo(EntryAssemblyLocation.FullName)?.ProductVersion
            : null;
    }

    public static FileInfo EntryAssemblyLocation { get; }
    public static string EntryAssemblyName { get; }
    public static string ProductVersion { get; }

    private static Assembly EntryAssembly { get; }
}