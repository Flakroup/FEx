using FEx.Platforms.Windows.Extensions;
using Shouldly;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Xunit;

namespace FEx.Platforms.Windows.Tests;

// Runs the real access-check script through Windows PowerShell against temp directories.
[SupportedOSPlatform("windows")]
public sealed class AccessCheckScriptWindowsTests : IDisposable
{
    // Would create this file in the working directory if the path ever reached the parser.
    private const string InjectionMarker = "fex-acl-injected";

    private readonly DirectoryInfo _parent = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

    [Theory]
    [InlineData("plain")]
    [InlineData("O'Brien")]
    [InlineData("x'; New-Item " + InjectionMarker + " #")]
    [InlineData("br[ack]ets")]
    [InlineData("%USERNAME%")]
    public void QueryAccess_ListsTheRootAndItsContent_WithoutRunningThePath(string name)
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Windows PowerShell and NTFS ACLs are Windows-only");

        var dir = _parent.CreateSubdirectory(name);
        File.WriteAllText(Path.Combine(dir.FullName, "file.txt"), "x");

        var lines = Query(dir);

        lines.Length.ShouldBe(2);
        lines[0].ShouldStartWith(dir.FullName + "|");
        AssertOwnedByOwner(lines);
        File.Exists(Path.Combine(Environment.CurrentDirectory, InjectionMarker)).ShouldBeFalse();
    }

    [Fact]
    public void QueryAccess_StillChecksAnEmptyDirectory()
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Windows PowerShell and NTFS ACLs are Windows-only");

        var dir = _parent.CreateSubdirectory("empty");

        var lines = Query(dir);

        lines.Length.ShouldBe(1);
        lines[0].ShouldStartWith(dir.FullName + "|");
        AssertOwnedByOwner(lines);
    }

    // The directory's own owner is passed as the "user", so every line must report it as owner;
    // an inverted -like or a missing environment would turn this False or fail the query.
    private static string[] Query(DirectoryInfo dir)
    {
        var everyone = (NTAccount)new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount));
        var owner = (NTAccount)dir.GetAccessControl().GetOwner(typeof(NTAccount))!;

        var lines = FileSystemExtensions.QueryAccess(dir, everyone, owner);

        lines.ShouldNotBeNull();

        return lines;
    }

    private static void AssertOwnedByOwner(string[] lines)
    {
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            parts.Length.ShouldBe(3, line);
            bool.TryParse(parts[1], out _).ShouldBeTrue(line);
            parts[2].ShouldBe("True", line);
        }
    }

    public void Dispose()
    {
        try
        {
            _parent.Delete(true);
        }
        catch (IOException)
        {
            // Best-effort temp cleanup.
        }
    }
}
