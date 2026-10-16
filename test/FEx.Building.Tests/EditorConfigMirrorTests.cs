using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// Pins FEx's opt-in to the DevConfigs <c>.editorconfig</c> mirror: the switch lives in the repository's own root
/// props, ahead of the DevConfigs import, and the mirrored copy stays out of git.
/// </summary>
public sealed class EditorConfigMirrorTests
{
    private const string Switch = "<DevConfigsMirrorEditorConfig>true</DevConfigsMirrorEditorConfig>";

    private static string RepositoryRoot { get; } = RepositoryLocator.FindRoot();

    [Fact]
    public void RootProps_EnableTheMirror_BeforeImportingDevConfigs()
    {
        var lines = File.ReadAllLines(Path.Combine(RepositoryRoot, "Directory.Build.props"));

        var switchLine = Array.FindIndex(lines, static line => line.Contains(Switch, StringComparison.Ordinal));
        var importLine = Array.FindIndex(lines, static line => line.Contains("DevConfigs", StringComparison.Ordinal)
                                                               && line.Contains("<Import", StringComparison.Ordinal));

        switchLine.ShouldBeGreaterThanOrEqualTo(0, "the root props must set DevConfigsMirrorEditorConfig to true");
        importLine.ShouldBeGreaterThanOrEqualTo(0);
        switchLine.ShouldBeLessThan(importLine, "the switch is read while DevConfigs/Directory.Build.targets evaluates, so it must precede the import");
    }

    [Fact]
    public void MirroredRootFile_IsGitIgnored()
    {
        File.ReadAllLines(Path.Combine(RepositoryRoot, ".gitignore"))
            .Select(static line => line.Trim())
            .ShouldContain("/.editorconfig");
    }
}
