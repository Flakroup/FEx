using FEx.Platforms.Windows.Extensions;
using Shouldly;
using System.Diagnostics;
using Xunit;

namespace FEx.FileSystem.Tests;

// #77: the ACL check used to splice the directory path and account names into single-quoted
// PowerShell literals, so an apostrophe in any of them ended the literal and ran the rest as code.
public sealed class AccessCheckScriptTests
{
    private const string MaliciousPath = @"C:\Users\O'Brien'; Remove-Item C:\ -Recurse; '\Docs";
    private const string MaliciousAccount = "DOMAIN\\o'brien'; calc; '";

    [Fact]
    public void Script_CarriesNoQuotedValueSlot()
    {
        // The only single quotes left are the two '|' separators the output parser splits on.
        FileSystemExtensions.AccessCheckScript.Split('\'').Length.ShouldBe(5);
    }

    [Fact]
    public void Script_ReadsEveryValueFromTheEnvironment()
    {
        var script = FileSystemExtensions.AccessCheckScript;

        script.ShouldContain($"-LiteralPath $env:{FileSystemExtensions.AccessCheckPathVariable}");
        script.ShouldContain($"$env:{FileSystemExtensions.AccessCheckEveryoneVariable}");
        script.ShouldContain($"$env:{FileSystemExtensions.AccessCheckUserVariable}");
    }

    [Fact]
    public void SetAccessCheckEnvironment_PassesValuesVerbatimOutsideTheScript()
    {
        var startInfo = new ProcessStartInfo("powershell");

        FileSystemExtensions.SetAccessCheckEnvironment(startInfo, MaliciousPath, MaliciousAccount, MaliciousAccount);

        startInfo.Environment[FileSystemExtensions.AccessCheckPathVariable].ShouldBe(MaliciousPath);
        startInfo.Environment[FileSystemExtensions.AccessCheckEveryoneVariable].ShouldBe(MaliciousAccount);
        startInfo.Environment[FileSystemExtensions.AccessCheckUserVariable].ShouldBe(MaliciousAccount);
        startInfo.Arguments.ShouldBeEmpty();
    }
}
