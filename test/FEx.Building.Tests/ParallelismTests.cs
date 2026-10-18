using Shouldly;
using System;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The translations from one thread cap to the MSBuild switch, the child-process environment and the
/// <c>dotnet test</c> options. A null cap must produce NOTHING: that is what keeps a server's command lines
/// and environment exactly what they were before the cap existed.
/// </summary>
public sealed class ParallelismTests
{
    private const string Root = @"X:\Repo";

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(8, 6)]
    [InlineData(16, 12)]
    [InlineData(32, 24)]
    [InlineData(64, 48)]
    public void LocalDefault_KeepsAQuarterOfTheMachineFree_AndAtLeastTwoThreads(int threads, int expected) =>
        Parallelism.LocalDefault(threads).ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Resolve_TreatsNullAndZeroAsUnbounded(int? requested) =>
        Parallelism.Resolve(requested).ShouldBeNull();

    [Fact]
    public void Resolve_KeepsAPositiveValue() =>
        Parallelism.Resolve(5).ShouldBe(5);

    [Fact]
    public void Resolve_RejectsANegativeValue_NamingTheParameter() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Parallelism.Resolve(-1))
            .ParamName.ShouldBe("MaxParallelism");

    [Fact]
    public void MsBuildSwitch_IsBareWithoutACap() =>
        Parallelism.MsBuildSwitch(null).ShouldBe("-m");

    [Fact]
    public void MsBuildSwitch_CarriesTheCap() =>
        Parallelism.MsBuildSwitch(6).ShouldBe("-m:6");

    [Fact]
    public void MsBuildArguments_AreEmptyWithoutACap() =>
        Parallelism.MsBuildArguments(null).ShouldBeEmpty();

    [Fact]
    public void MsBuildArguments_CarryTheCap() =>
        Parallelism.MsBuildArguments(6).ShouldBe(["-maxcpucount:6"]);

    [Fact]
    public void Environment_IsEmptyWithoutACap() =>
        Parallelism.Environment(null, Root).ShouldBeEmpty();

    [Fact]
    public void Environment_GivesTheCapItsOwnProcessorCountAndCompilerServer()
    {
        var environment = Parallelism.Environment(6, Root);

        environment.Count.ShouldBe(2);
        environment["DOTNET_PROCESSOR_COUNT"].ShouldBe("6");
        environment["SharedCompilationId"].ShouldMatch("^fex-p6-[0-9A-F]{8}$");
    }

    [Fact]
    public void Environment_GivesEachRepositoryItsOwnCompilerServer_BecauseEachMayResolveAnotherSdk() =>
        Parallelism.Environment(6, Root)["SharedCompilationId"]
            .ShouldNotBe(Parallelism.Environment(6, @"X:\Other")["SharedCompilationId"]);

    [Fact]
    public void Environment_KeepsOneRepositorysCompilerServerAcrossBuildsAndPathCase() =>
        Parallelism.Environment(6, Root)["SharedCompilationId"]
            .ShouldBe(Parallelism.Environment(6, Root.ToLowerInvariant())["SharedCompilationId"]);

    [Theory]
    [InlineData(24, 0xFFFF_FFFFL, 0xFFFF_FF00L)]
    [InlineData(6, 0xFFL, 0xFCL)]
    [InlineData(2, 0b1010_0101L, 0b1010_0000L)]
    [InlineData(1, -1L, long.MinValue)]
    public void AffinityMask_KeepsTheHighestProcessorsTheCurrentMaskAllows(int cap, long current, long expected) =>
        Parallelism.AffinityMask(cap, current).ShouldBe(expected);

    [Theory]
    [InlineData(null, 0xFFFF_FFFFL)]
    [InlineData(0, 0xFFFF_FFFFL)]
    [InlineData(-1, 0xFFFF_FFFFL)]
    [InlineData(8, 0xFFL)]
    [InlineData(32, 0xFFFFL)]
    [InlineData(4, 0b1010_0101L)]
    public void AffinityMask_LeavesTheAffinityAlone_WithoutAPositiveCapOrWithNoMoreProcessorsThanIt(int? cap, long current) =>
        Parallelism.AffinityMask(cap, current).ShouldBeNull();

    [Fact]
    public void TestArguments_AreEmptyWithoutACap() =>
        Parallelism.TestArguments(null, ["--max-threads", "2"]).ShouldBeEmpty();

    [Fact]
    public void TestArguments_CapTheModulesAndTheThreads() =>
        Parallelism.TestArguments(6, null)
            .ShouldBe(["--max-parallel-test-modules", "6", "--max-threads", "6"]);

    [Fact]
    public void TestArguments_LeaveOutAnOptionTheRepositoryAlreadyCarries() =>
        Parallelism.TestArguments(6, ["--max-threads", "2"])
            .ShouldBe(["--max-parallel-test-modules", "6"]);

    [Theory]
    [InlineData("--max-threads:2")]
    [InlineData("--max-threads=2")]
    public void TestArguments_RecogniseTheDelimitedFormsMtpParses(string threads) =>
        Parallelism.TestArguments(6, [threads, "--max-parallel-test-modules=1"]).ShouldBeEmpty();

    [Fact]
    public void TestArguments_DoNotMistakeAnotherOptionForTheirs() =>
        Parallelism.TestArguments(6, ["--max-threads-extra", "2"])
            .ShouldBe(["--max-parallel-test-modules", "6", "--max-threads", "6"]);

    [Fact]
    public void TestArguments_LeaveOutBothWhenBothArePresent() =>
        Parallelism.TestArguments(6, ["--max-threads", "2", "--max-parallel-test-modules", "1"])
            .ShouldBeEmpty();
}
