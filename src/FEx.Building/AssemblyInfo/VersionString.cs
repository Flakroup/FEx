using System;
using System.Text.RegularExpressions;

namespace FEx.Building.AssemblyInfo;

public class VersionString
{
    public string Major { get; set; }
    public string Minor { get; set; }
    public string Build { get; set; }
    public string Revision { get; set; }

    private static Regex VersionRegex { get; } =
        new(@"^(?<Major>\d+)\.(?<Minor>\d+)\.(?:(?:(?<Build>\d+)\.(?<Revision>\*|\d+))|(?<Build>\*|\d+))$",
            RegexOptions.Compiled);

    public VersionString()
    {
        Major = "0";
        Minor = "0";
        Build = "0";
        Revision = "0";
    }

    public VersionString(string version)
    {
        Major = Minor = Build = Revision = "0";

        if (!Parse(version))
            throw new ArgumentException("Invalid version string");
    }

    public static bool TryParse(string input, out VersionString? version)
    {
        var temp = new VersionString();
        version = null;

        if (temp.Parse(input))
            version = temp;

        return version is not null;
    }

    public override string ToString() =>
        string.Format("{0}.{1}{2}{3}",
            Major,
            Minor,
            string.IsNullOrEmpty(Build)
                ? ""
                : "." + Build,
            string.IsNullOrEmpty(Revision)
                ? ""
                : "." + Revision);

    private bool Parse(string input)
    {
        var match = VersionRegex.Match(input);

        if (match.Success)
        {
            Major = match.Groups["Major"].Value;
            Minor = match.Groups["Minor"].Value;
            Build = match.Groups["Build"].Value;
            Revision = match.Groups["Revision"].Value;
        }

        return match.Success;
    }
}