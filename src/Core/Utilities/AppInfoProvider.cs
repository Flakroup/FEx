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
    // IAppInfoProvider declares these as non-null, but they are only resolvable on Windows with a managed
    // entry assembly; the null! default keeps the non-null contract while the runtime value may be null.
    public string EntryAssemblyName { get; } = null!;
    public Assembly EntryAssembly { get; } = null!;
    public FileInfo EntryAssemblyLocation { get; } = null!;

    public string Name { get; }

    // Assigned in the ctor; the null! initializer satisfies definite-assignment on netstandard while the
    // ctor supplies the real (possibly-null on some hosts) value, honoring the non-null interface contract.
    public Version Version { get; } = null!;
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

    public string? LogDirPath { get; }
    public string? LogFilePath { get; }

    // Assigned (guarded non-null) in the ctor; the null! initializer satisfies definite-assignment on netstandard.
    public IAppInfo AppInfo { get; } = null!;

    private FileVersionInfo? ProductVersionInfo { get; }

    public AppInfoProvider(IAppInfo appInfo)
    {
        AppInfo = appInfo.Guard(nameof(appInfo));

        if (PlatformInfoProvider.IsWindows)
        {
            // IAppInfoProvider declares these as non-null; on hosts without a managed entry assembly they
            // may actually be null at runtime (unchanged from before nullable was enabled).
            EntryAssembly = Assembly.GetEntryAssembly()!;

            var mainModule = Process.GetCurrentProcess().MainModule?.FileName;

            EntryAssemblyLocation = EntryAssembly?.Location is not null ? new(EntryAssembly.Location) :
                mainModule is not null ? new FileInfo(mainModule) : null!;

            EntryAssemblyName = EntryAssembly?.GetName().Name!;

            ProductVersionInfo = EntryAssemblyLocation is not null
                ? FileVersionInfo.GetVersionInfo(EntryAssemblyLocation.FullName)
                : null;
        }

        // Name/Version honor the non-null interface contract; the coalesced tail may still be null on hosts
        // without an entry assembly (unchanged runtime behavior).
        Name = (AppInfo!.Name
                ?? TryGetProductName() ?? EntryAssemblyName ?? EntryAssembly?.EntryPoint?.DeclaringType?.Namespace)!;

        Version = (AppInfo?.Version
                   ?? ParseVersionString(ProductVersionInfo?.ProductVersion) ?? EntryAssembly?.GetName().Version)!;

        VersionString = Version?.ToString()!;

        NameAndVersion = $"{Name} {Version}";
        NameAndVersionWithPrefix = $"{Name} ver. {Version}";
        NameLineVersion = $"{Name}{Environment.NewLine}{Version}";
        NameLineVersionWithPrefix = $"{Name}{Environment.NewLine}ver. {Version}";

        // The attribute passed to the selector may be null (no such attribute); the null flows through as the
        // attribute value, matching the pre-nullable behavior.
        Company = AppInfo?.Company ?? GetEntryAssemblyAttribute<AssemblyCompanyAttribute>(x => x?.Company!);

        Copyright = GetEntryAssemblyAttribute<AssemblyCopyrightAttribute>(x => x?.Copyright!);

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

        UserSettingsPath = Path.Combine(UserDataPath, $"{Name}.config");
    }

    private static Version? ParseVersionString(string? version) =>
        Version.TryParse(version, out var result)
            ? result
            : null;

    private string? TryGetProductName() =>
        !string.IsNullOrEmpty(ProductVersionInfo?.ProductName)
            ? ProductVersionInfo?.ProductName
            : null;

    private string GetEntryAssemblyAttribute<T>(Func<T, string> func) where T : Attribute =>
        EntryAssembly.GetEntryAssemblyAttribute(func);
}