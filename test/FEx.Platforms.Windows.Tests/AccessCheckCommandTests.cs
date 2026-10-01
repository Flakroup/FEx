using FEx.Platforms.Windows.Extensions;
using Shouldly;
using System;
using Xunit;

namespace FEx.Platforms.Windows.Tests;

// #77: the ACL check used to splice the path and account names into single-quoted PowerShell
// literals, and icacls ran through cmd /C, which expands %VAR% even inside quotes. These pin the
// command shapes; AccessCheckScriptWindowsTests runs the real script.
public sealed class AccessCheckCommandTests
{
    private const string MaliciousPath = @"C:\Users\O'Brien'; Remove-Item C:\ -Recurse; '\%USERNAME%";
    private const string MaliciousAccount = "DOMAIN\\o'brien'; calc; '";

    [Fact]
    public void Script_ListsTheRootAndReadsEveryValueFromTheEnvironment()
    {
        var script = FileSystemExtensions.AccessCheckScript;

        script.ShouldContain($"(@(Get-Item -LiteralPath $env:{FileSystemExtensions.AccessCheckPathVariable})");
        script.ShouldContain($"Get-ChildItem -LiteralPath $env:{FileSystemExtensions.AccessCheckPathVariable} -Recurse");
        script.ShouldContain($"$env:{FileSystemExtensions.AccessCheckEveryoneVariable}");
        script.ShouldContain($"$env:{FileSystemExtensions.AccessCheckUserVariable}");
    }

    [Fact]
    public void CreateAccessCheckCmd_PassesValuesThroughTheEnvironmentOnly()
    {
        using var cmd = FileSystemExtensions.CreateAccessCheckCmd(MaliciousPath, MaliciousAccount, MaliciousAccount);
        var startInfo = cmd.StartInfo;

        startInfo.FileName.ShouldBe("powershell");
        startInfo.Environment[FileSystemExtensions.AccessCheckPathVariable].ShouldBe(MaliciousPath);
        startInfo.Environment[FileSystemExtensions.AccessCheckEveryoneVariable].ShouldBe(MaliciousAccount);
        startInfo.Environment[FileSystemExtensions.AccessCheckUserVariable].ShouldBe(MaliciousAccount);
        startInfo.Environment.ContainsKey("PSModulePath").ShouldBeFalse();
        startInfo.Arguments.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(@"C:\data\%USERNAME%", "\"C:\\data\\%USERNAME%\"")]
    [InlineData(@"C:\a b\O'Brien", "\"C:\\a b\\O'Brien\"")]
    [InlineData(@"C:\", "\"C:\\\\\"")]
    public void QuoteArgument_KeepsThePathOneLiteralArgument(string path, string expected) =>
        FileSystemExtensions.QuoteArgument(path).ShouldBe(expected);

    [Fact]
    public void QuoteArgument_RejectsAnEmbeddedQuote() =>
        Should.Throw<ArgumentException>(() => FileSystemExtensions.QuoteArgument("C:\\a\"b"));

    [Theory]
    [InlineData(@"C:\data\%USERNAME%", false)]
    [InlineData(@"C:\data\100%", false)]
    [InlineData(@"C:\data\O'Brien & [x]", true)]
    public void CanPassThroughCmd_RefusesWhatCmdWouldExpand(string path, bool expected) =>
        FileSystemExtensions.CanPassThroughCmd(path).ShouldBe(expected);
}
