using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FEx.Utilities;

public static class AppInfo
{
    public static FileInfo EntryAssemblyLocation { get; }
    public static string EntryAssemblyName { get; }
    public static string ProductVersion { get; }
    public static string ApplicationName { get; }

    private static FileVersionInfo ProductVersionInfo { get; }

    private static Assembly EntryAssembly { get; }

    static AppInfo()
    {
        EntryAssembly = Assembly.GetEntryAssembly();
        EntryAssemblyLocation = EntryAssembly?.Location is not null
            ? new FileInfo(EntryAssembly.Location)
            : null;
        EntryAssemblyName = EntryAssembly?.GetName().Name;
        ProductVersionInfo = EntryAssemblyLocation is not null
            ? FileVersionInfo.GetVersionInfo(EntryAssemblyLocation.FullName)
            : null;
        ProductVersion = ProductVersionInfo?.ProductVersion;
        ApplicationName = ProductVersionInfo?.ProductName ?? EntryAssembly?.EntryPoint?.DeclaringType?.Namespace;
    }
}