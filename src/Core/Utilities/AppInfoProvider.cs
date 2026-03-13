using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FEx.Core.Utilities;

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
    public string NameLineVersionWithPrefix { get; }
    public string NameAndVersion { get; }

    public DirectoryInfo UserData { get; }
    public string UserDataPath { get; }

    public DirectoryInfo AppData { get; }
    public string AppDataPath { get; }

    public string UserSettingsPath { get; }

    public string LogDirPath { get; }
    public string LogFilePath { get; }

    public IAppInfo AppInfo { get; }

    private FileVersionInfo ProductVersionInfo { get; }

    public AppInfoProvider(IAppInfo appInfo)
    {
        AppInfo = appInfo;

        if (PlatformInfoProvider.IsWindows)
        {
            EntryAssembly = Assembly.GetEntryAssembly();

            var mainModule = Process.GetCurrentProcess().MainModule?.FileName;

            EntryAssemblyLocation = EntryAssembly?.Location is not null ? new(EntryAssembly.Location) :
                mainModule is not null ? new FileInfo(mainModule) : null;

            EntryAssemblyName = EntryAssembly?.GetName().Name;

            ProductVersionInfo = EntryAssemblyLocation is not null
                ? FileVersionInfo.GetVersionInfo(EntryAssemblyLocation.FullName)
                : null;
        }

        Name = AppInfo!.Name
               ?? TryGetProductName() ?? EntryAssemblyName ?? EntryAssembly?.EntryPoint?.DeclaringType?.Namespace;

        Version = AppInfo?.Version
                  ?? ParseVersionString(ProductVersionInfo?.ProductVersion) ?? EntryAssembly?.GetName().Version;

        VersionString = Version?.ToString();

        NameAndVersion = $"{Name} {Version}";
        NameAndVersionWithPrefix = $"{Name} ver. {Version}";
        NameLineVersion = $"{Name}{Environment.NewLine}{Version}";
        NameLineVersionWithPrefix = $"{Name}{Environment.NewLine}ver. {Version}";

        Company = AppInfo?.Company ?? GetEntryAssemblyAttribute<AssemblyCompanyAttribute>(x => x?.Company);

        Copyright = GetEntryAssemblyAttribute<AssemblyCopyrightAttribute>(x => x?.Copyright);

        UserData = AppInfo?.UserData
                   ?? (PlatformInfoProvider.IsWindows
                       ? new(Environment.SpecialFolder.ApplicationData.GetSpecialDirectoryPathDescendants(
                           Company.Guard(nameof(Company)),
                           Name))
                       : Environment.SpecialFolder.UserProfile.GetSpecialDirectory().Directory);

        AppData = AppInfo?.AppData
                  ?? (PlatformInfoProvider.IsWindows
                      ? new(Environment.SpecialFolder.CommonApplicationData.GetSpecialDirectoryPathDescendants(
                          Company.Guard(nameof(Company)),
                          Name))
                      : Environment.SpecialFolder.LocalApplicationData.GetSpecialDirectory().Directory);

        UserDataPath = UserData.FullName;
        UserData.Create();

        AppDataPath = AppData.FullName;
        AppData.Create();

        UserSettingsPath = UserDataPath is not null
            ? Path.Combine(UserDataPath, $"{Name}.config")
            : null;
    }

    private static Version ParseVersionString(string version) =>
        Version.TryParse(version, out var result)
            ? result
            : null;

    private string TryGetProductName() =>
        !string.IsNullOrEmpty(ProductVersionInfo?.ProductName)
            ? ProductVersionInfo.ProductName
            : null;

    private string GetEntryAssemblyAttribute<T>(Func<T, string> func) where T : Attribute =>
        EntryAssembly.GetEntryAssemblyAttribute(func);
}