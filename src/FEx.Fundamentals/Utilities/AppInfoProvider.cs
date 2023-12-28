using FEx.Abstractions;
using FEx.Basics.Extensions;
using FEx.Extensions;
using FEx.Extensions.Base.Helpers;
using FEx.Fundamentals.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FEx.Fundamentals.Utilities;

public record AppInfoProvider : IAppInfoProvider
{
    public string EntryAssemblyName { get; }
    public Assembly EntryAssembly { get; }
    public FileInfo EntryAssemblyLocation { get; }

    public string Name { get; }
    public Version Version { get; }
    public string VersionString { get; }

    public string Company { get; }
    public string Copyright { get; }
    public string NameAndVersionWithPrefix { get; }
    public string NameLineVersion { get; }
    public string NameAndVersion { get; }

    public DirectoryInfo UserData { get; }
    public string UserDataPath { get; }

    public DirectoryInfo AppData { get; }
    public string AppDataPath { get; }

    public string UserSettingsPath { get; }

    public string LogFilePath { get; }

    private static AppInfo AppInfo { get; set; }
    private FileVersionInfo ProductVersionInfo { get; }

    public AppInfoProvider()
    {
        if (PlatformInfoProvider.IsWindows)
        {
            EntryAssembly = Assembly.GetEntryAssembly();

            string mainModule = Process.GetCurrentProcess().MainModule?.FileName;

            EntryAssemblyLocation = EntryAssembly?.Location is not null ? new FileInfo(EntryAssembly.Location) :
                mainModule is not null ? new FileInfo(mainModule) : null;

            EntryAssemblyName = EntryAssembly?.GetName().Name;

            ProductVersionInfo = EntryAssemblyLocation is not null
                ? FileVersionInfo.GetVersionInfo(EntryAssemblyLocation.FullName)
                : null;
        }

        Name = AppInfo?.Name ?? TryGetProductName() ?? EntryAssembly?.EntryPoint?.DeclaringType?.Namespace;

        Version = AppInfo?.Version
                  ?? ParseVersionString(ProductVersionInfo?.ProductVersion) ?? EntryAssembly?.GetName().Version;

        VersionString = Version?.ToString();

        NameAndVersion = $"{Name} {Version}";
        NameAndVersionWithPrefix = $"{Name} ver. {Version}";
        NameLineVersion = $"{Name}{Environment.NewLine}{Version}";

        Company =
            AppInfo?.Company ?? GetEntryAssemblyAttribute<AssemblyCompanyAttribute>(x => x?.Company);

        Copyright = GetEntryAssemblyAttribute<AssemblyCopyrightAttribute>(x => x?.Copyright);

        if (PlatformInfoProvider.IsWindows)
        {
            UserData = new DirectoryInfo(
                Environment.SpecialFolder.ApplicationData.GetSpecialDirectoryPathDescendants(Company.Guard(
                    nameof(Company)), Name));

            AppData = new DirectoryInfo(
                Environment.SpecialFolder.CommonApplicationData.GetSpecialDirectoryPathDescendants(Company.Guard(
                    nameof(Company)), Name));
        }
        else
        {
            UserData = Environment.SpecialFolder.UserProfile.GetSpecialDirectory().Directory;
            AppData = Environment.SpecialFolder.LocalApplicationData.GetSpecialDirectory().Directory;
        }

        UserDataPath = UserData.FullName;
        UserData.Create();

        AppDataPath = AppData.FullName;
        AppData.Create();

        LogFilePath = Path.GetFullPath(Path.Combine(UserDataPath, ".logs", $"{Name}.log"));

        UserSettingsPath = UserDataPath is not null
            ? Path.Combine(UserDataPath, $"{Name}.config")
            : null;
    }

    public static void Initialize(AppInfo appInfo)
    {
        AppInfo ??= appInfo;
    }

    private static Version ParseVersionString(string version) => Version.TryParse(version, out Version result)
        ? result
        : null;

    private string TryGetProductName() => !string.IsNullOrEmpty(ProductVersionInfo?.ProductName)
        ? ProductVersionInfo.ProductName
        : null;

    private string GetEntryAssemblyAttribute<T>(Func<T, string> func) where T : Attribute =>
        EntryAssembly.GetEntryAssemblyAttribute(func);
}