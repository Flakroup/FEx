using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// Pins the shape of <c>build/CoverageDebt.cs</c>, the ratchet's list of files exempt from the 100% gate: it
/// may only name single files, once each, that still exist - otherwise it stops shrinking honestly.
/// </summary>
public sealed class CoverageDebtTests
{
    private static string RepositoryRoot { get; } = FindRepositoryRoot();

    [Fact]
    public void Debt_NamesFilesOnly_NeverADirectory()
    {
        foreach (var entry in CoverageDebt.Files)
        {
            Path.GetExtension(entry).ShouldBe(".cs", $"'{entry}' must name one source file - a directory exempts new files too");
            Directory.Exists(Path.Combine(RepositoryRoot, entry)).ShouldBeFalse(entry);
        }
    }

    [Fact]
    public void Debt_HasNoDuplicates()
    {
        CoverageDebt.Files.GroupBy(static entry => entry, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Debt_PointsAtExistingFiles()
    {
        CoverageDebt.Files.Where(entry => !File.Exists(Path.Combine(RepositoryRoot, entry)))
            .ShouldBeEmpty("a covered or deleted file must leave the list, not linger in it");
    }

    [Fact]
    public void Debt_IsRootRelativeUnderSrcWithForwardSlashes()
    {
        CoverageDebt.Files.ShouldAllBe(static entry => entry.StartsWith("src/", StringComparison.Ordinal)
                                                       && !entry.Contains('\\'));
    }

    [Fact]
    public void Debt_IsSorted()
    {
        CoverageDebt.Files.ShouldBe(CoverageDebt.Files.OrderBy(static entry => entry, StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void ModulesWithoutExecutableCode_AreRealProjectsListedOnce()
    {
        CoverageDebt.ModulesWithoutExecutableCode.Distinct(StringComparer.OrdinalIgnoreCase)
            .Count().ShouldBe(CoverageDebt.ModulesWithoutExecutableCode.Count);

        var projects = Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(static path => Path.GetFileNameWithoutExtension(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        CoverageDebt.ModulesWithoutExecutableCode.Where(module => !projects.Contains(module)).ShouldBeEmpty();
    }

    [Fact]
    public void Build_DeclaresTheCoverageGateAndFeedsItTheDebtList()
    {
        var build = File.ReadAllText(Path.Combine(RepositoryRoot, "build", "Build.cs"));

        build.ShouldContain("ICoverageTarget");
        build.ShouldContain("CoverageExclusions => CoverageDebt.Files");
        build.ShouldContain("ModulesWithoutExecutableCode => CoverageDebt.ModulesWithoutExecutableCode");
        build.ShouldContain(".DependsOn(((ICoverageTarget)this).Coverage)");
    }

    [Fact]
    public void Ci_RunsCoverageInPlaceOfTest_AndPublishDoesNotRerunIt()
    {
        var ci = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "ci.yml"));

        ci.ShouldContain("build.ps1 Coverage Inspect");
        ci.ShouldContain("build.ps1 Coverage Inspect Pack");
        ci.ShouldNotContain("build.ps1 Test");
        ci.ShouldContain("--skip Restore Compile Test Coverage Verify Pack");
        ci.ShouldContain("artifacts/test-results/*.cobertura.xml");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FEx.slnx")))
                return directory.FullName;
        }

        throw new InvalidOperationException("FEx.slnx not found above the test output directory.");
    }
}
