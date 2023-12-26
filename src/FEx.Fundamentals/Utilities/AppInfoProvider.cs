using FEx.Abstractions;
using FEx.Basics.Extensions;
using FEx.Extensions.Base.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FEx.Fundamentals.Utilities;

public class AppInfoProvider : IAppInfoProvider
{
    public Assembly EntryAssembly { get; set; }
    public string EntryAssemblyLocation { get; set; }
    public string ApplicationName { get; }
    public string ProductVersion { get; private set; }
    public Version ApplicationVersion { get; }
    public string ApplicationNameAndVersionWithPrefix { get; private set; }
    public string ApplicationNameLineVersion { get; private set; }
    public string ApplicationNameAndVersion { get; private set; }
    public string ApplicationCompany { get; }
    public string ApplicationCopyright { get; private set; }
    public string UserDataPath => UserData.FullName;
    public DirectoryInfo UserData { get; }
    public string AppDataPath => AppData.FullName;
    public DirectoryInfo AppData { get; }
    public string UserSettingsPath { get; private set; }

    public AppInfoProvider()
    {
        EntryAssembly = Assembly.GetEntryAssembly();
        EntryAssemblyLocation = Process.GetCurrentProcess().MainModule?.FileName;

        ApplicationName = EntryAssembly?.EntryPoint?.DeclaringType?.Namespace;

        ProductVersion = EntryAssemblyLocation is not null
            ? FileVersionInfo.GetVersionInfo(EntryAssemblyLocation)?.ProductVersion
            : null;

        ApplicationVersion = EntryAssembly?.GetName()?.Version;
        ApplicationCompany = GetEntryAssemblyAttribute<AssemblyCompanyAttribute>(x => x?.Company);
        ApplicationCopyright = GetEntryAssemblyAttribute<AssemblyCopyrightAttribute>(x => x?.Copyright);
        ApplicationNameAndVersion = $"{ApplicationName} {ApplicationVersion}";
        ApplicationNameAndVersionWithPrefix = $"{ApplicationName} ver. {ApplicationVersion}";
        ApplicationNameLineVersion = $"{ApplicationName}{Environment.NewLine}{ApplicationVersion}";

        UserData = new DirectoryInfo(
            Environment.SpecialFolder.ApplicationData.GetSpecialDirectoryPathDescendants(ApplicationCompany,
                ApplicationName));

        UserData.Create();

        AppData = new DirectoryInfo(
            Environment.SpecialFolder.CommonApplicationData.GetSpecialDirectoryPathDescendants(ApplicationCompany,
                ApplicationName));

        AppData.Create();

        UserSettingsPath = UserDataPath is not null
            ? Path.Combine(UserDataPath, $"{ApplicationName}.config")
            : null;
    }

    private string GetEntryAssemblyAttribute<T>(Func<T, string> func) where T : Attribute =>
        EntryAssembly.GetEntryAssemblyAttribute(func);
}