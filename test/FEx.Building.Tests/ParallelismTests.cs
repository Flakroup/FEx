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
        Parallelism.Environment(null).ShouldBeEmpty();

    [Fact]
    public void Environment_GivesTheCapItsOwnProcessorCountAndCompilerServer()
    {
        var environment = Parallelism.Environment(6);

        environment.Count.ShouldBe(2);
        environment["DOTNET_PROCESSOR_COUNT"].ShouldBe("6");
        environment["SharedCompilationId"].ShouldBe("fex-p6");
    }

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

    [Fact]
    public void TestArguments_LeaveOutBothWhenBothArePresent() =>
        Parallelism.TestArguments(6, ["--max-threads", "2", "--max-parallel-test-modules", "1"])
            .ShouldBeEmpty();
}
