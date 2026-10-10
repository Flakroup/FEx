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
    private const string CorpusDirectory = "test/FEx.Analyzers.Tests/CorpusData";

    [Fact]
    public void The_corpus_directory_exists_at_the_path_the_exclusion_names()
    {
        var corpus = Path.Combine(FindRepositoryRoot(), "test", "FEx.Analyzers.Tests", "CorpusData");

        Directory.Exists(corpus).ShouldBeTrue();
        File.Exists(Path.Combine(corpus, "expected.json")).ShouldBeTrue();
    }

    [Theory]
    [InlineData(CorpusDirectory + "/StyleA.cs")]
    [InlineData(CorpusDirectory + "/StyleB.cs")]
    [InlineData(CorpusDirectory + "/Stubs.cs")]
    public void Codacy_excludes_every_corpus_source_file(string repositoryRelativePath) =>
        ExcludedGlobs().Any(glob => Covers(glob, repositoryRelativePath)).ShouldBeTrue();

    [Fact]
    public void The_config_file_opens_with_the_document_marker_codacy_requires() =>
        File.ReadLines(Path.Combine(FindRepositoryRoot(), ".codacy.yml")).First().Trim().ShouldBe("---");

    // Codacy's globs are Java globs; the entry in use is a directory prefix ending in "/**", the only shape handled here.
    private static bool Covers(string glob, string path) =>
        glob.EndsWith("/**", StringComparison.Ordinal) && path.StartsWith(glob[..^2], StringComparison.Ordinal);

    private static string[] ExcludedGlobs()
    {
        var lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), ".codacy.yml"));
        return lines
            .SkipWhile(line => line.TrimEnd() != "exclude_paths:")
            .Skip(1)
            .TakeWhile(line => line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
            .Select(line => line.TrimStart()[2..].Trim().Trim('"', '\''))
            .ToArray();
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
