using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FEx.Agnostics.Helpers;

/// <summary>Default <see cref="IAppVersionProvider"/> that reads the product version of the entry assembly or, failing that, of the main process module.</summary>
public class DefaultAppVersionProvider : IAppVersionProvider
{
    /// <summary>Resolves the application version from the file version info of the entry assembly or main module.</summary>
    /// <returns>The product version, falling back to the assembly version, or an empty string if none can be determined.</returns>
    public virtual string GetAppVersion()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            var mainModule = Process.GetCurrentProcess().MainModule?.FileName;

#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
            var entryAssemblyLocation = entryAssembly?.Location;
#pragma warning restore IL3000 // Avoid accessing Assembly file path when publishing as a single file
            var versionedAssemblyLocation = !entryAssemblyLocation.IsNullOrEmptyString() ? new(entryAssemblyLocation) :
                mainModule is not null ? new FileInfo(mainModule) : null;

            var productVersionInfo = versionedAssemblyLocation is not null
                ? FileVersionInfo.GetVersionInfo(versionedAssemblyLocation.FullName)
                : null;

            // Interface contract is non-null; fall back to empty when no version is resolvable (as the catch does).
            return productVersionInfo?.ProductVersion ?? entryAssembly?.GetName().Version?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}