using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FEx.Agnostics.Helpers;

public class DefaultAppVersionProvider : IAppVersionProvider
{
    public virtual string GetAppVersion()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            string mainModule = Process.GetCurrentProcess().MainModule?.FileName;

#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
            string entryAssemblyLocation = entryAssembly?.Location;
#pragma warning restore IL3000 // Avoid accessing Assembly file path when publishing as a single file
            FileInfo versionedAssemblyLocation = !entryAssemblyLocation.IsNullOrEmpty() ? new(entryAssemblyLocation) :
                mainModule is not null ? new FileInfo(mainModule) : null;

            FileVersionInfo productVersionInfo = versionedAssemblyLocation is not null
                ? FileVersionInfo.GetVersionInfo(versionedAssemblyLocation.FullName)
                : null;

            return productVersionInfo?.ProductVersion ?? entryAssembly?.GetName().Version?.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }
}