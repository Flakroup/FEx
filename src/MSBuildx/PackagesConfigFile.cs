using NuGet.Packaging.Core;
using NuGet.Versioning;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace FEx.MSBuildx;

/// <summary>
/// Reads the package identities listed in a legacy <c>packages.config</c> file.
/// </summary>
internal static class PackagesConfigFile
{
    /// <summary>
    /// Reads every package entry of <paramref name="file" />, keeping entries that repeat a package id (multi-TFM or
    /// multi-version listings) and rejecting inline DTDs.
    /// </summary>
    /// <param name="file">The <c>packages.config</c> file; a missing file yields no packages.</param>
    /// <returns>The package identities in file order.</returns>
    /// <exception cref="XmlException">The file is not well-formed XML, declares a DTD or has an entry without id or version.</exception>
    public static IReadOnlyList<PackageIdentity> Read(FileInfo file)
    {
        if (!file.Exists)
            return [];

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        using var stream = file.OpenRead();
        using var xmlReader = XmlReader.Create(stream, settings);

        // PackagesConfigReader rejects repeated ids (even with allowDuplicatePackageIds when id and version both
        // repeat), which NuGet.Core accepted, so the entries are read directly.
        return XDocument.Load(xmlReader)
            .Descendants("package")
            .Select(x => new PackageIdentity(
                (string?)x.Attribute("id") ?? throw new XmlException("A package entry has no id attribute."),
                NuGetVersion.Parse((string?)x.Attribute("version") ?? throw new XmlException("A package entry has no version attribute."))))
            .ToList();
    }
}
