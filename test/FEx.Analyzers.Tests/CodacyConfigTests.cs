using System;
using System.IO;
using System.Linq;
using Shouldly;
using Xunit;

namespace FEx.Analyzers.Tests;

/// <summary>
/// The golden corpus violates rules on purpose, so Codacy must not analyze it: every finding there is noise that buries
/// the real ones on a pull request. Codacy reads <c>exclude_paths</c> from the root <c>.codacy.yml</c> and nothing else
/// notices when the entry stops matching the directory, so this pins the two to each other.
/// </summary>
public class CodacyConfigTests
{
    private const string TestProjectDirectory = "test/FEx.Analyzers.Tests";
    private const string CorpusDirectoryName = "CorpusData";

    // Pinned whole rather than parsed: a broader glob, an include_paths that re-includes the corpus, a byte order mark
    // or broken YAML each change what Codacy analyzes, and a line-by-line reader would wave every one of them through.
    [Fact]
    public void The_config_excludes_the_corpus_and_nothing_else()
    {
        var path = Path.Combine(FindRepositoryRoot(), ".codacy.yml");

        File.ReadAllBytes(path)[0].ShouldBe((byte)'-', "Codacy requires the file to open with the --- marker, no byte order mark");
        File.ReadAllLines(path).ShouldBe(["---", "exclude_paths:", $"  - \"{TestProjectDirectory}/{CorpusDirectoryName}/**\""]);
    }

    // Codacy's globs are case-sensitive and Windows paths are not, so the name is read from the listing, not probed.
    [Fact]
    public void The_corpus_directory_carries_exactly_the_name_the_exclusion_spells()
    {
        var project = Path.Combine(FindRepositoryRoot(), TestProjectDirectory);

        Directory.EnumerateDirectories(project).Select(Path.GetFileName).ShouldContain(CorpusDirectoryName);
        File.Exists(Path.Combine(project, CorpusDirectoryName, "expected.json")).ShouldBeTrue();
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FEx.slnx")))
                return directory.FullName;
        }

        throw new FileNotFoundException("FEx.slnx was not found above the test output directory.");
    }
}
