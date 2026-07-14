using FEx.Agnostics.BaseObjects;
using NuGet.Packaging.Core;
using System;

namespace FEx.NuGetx;

public class NuGetPackageInstallation : NotifyPropertyChanged
{
    public string Name { get; }
    public string? Version { get; }
    public Uri Url { get; }

    public NuGetPackageInstallation(PackageIdentity package)
    {
        Name = package.Id;

        Version = package.HasVersion
            ? package.Version.ToString()
            : null;

        Url = new("https://" + $"www.nuget.org/packages/{Name}{(Version is not null ? $"/{Version}" : string.Empty)}");
    }
}