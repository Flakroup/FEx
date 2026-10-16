using System.IO;
using Shouldly;
using Xunit;

namespace FEx.Analyzers.Tests;

/// <summary>
/// The golden corpus is the DevConfigs submodule's <c>analyzers-corpus</c>, embedded at build time. A copy kept in this
/// repository could go stale when the corpus is re-recorded, so the embedded files must be the submodule's own.
/// </summary>
public class CorpusSourceTests
{
    [Theory]
    [InlineData("StyleA.cs")]
    [InlineData("StyleB.cs")]
    [InlineData("Stubs.cs")]
    [InlineData("EmptyNs.cs")]
    [InlineData("expected.json")]
    public void The_embedded_corpus_file_is_the_one_in_the_DevConfigs_submodule(string name)
    {
        var path = Path.Combine(RepositoryLocator.FindRoot(), "DevConfigs", "analyzers-corpus", name);

        File.Exists(path).ShouldBeTrue($"{path} is missing: initialize the DevConfigs submodule (git submodule update --init)");
        GoldenCorpusTests.ReadResource(name).ShouldBe(File.ReadAllText(path));
    }

    [Fact]
    public void The_repository_keeps_no_copy_of_the_corpus() =>
        Directory.Exists(Path.Combine(RepositoryLocator.FindRoot(), "test", "FEx.Analyzers.Tests", "CorpusData")).ShouldBeFalse();
}
