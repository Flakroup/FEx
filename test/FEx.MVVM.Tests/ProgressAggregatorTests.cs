using FEx.MVVM.Utilities;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace FEx.MVVM.Tests;

public sealed class TestableProgressAggregator : ProgressAggregator
{
    public List<string> LoggedErrors { get; } = [];

    protected override void LogError(string message) => LoggedErrors.Add(message);

    public void InvokeUpdateProgressInfo() => UpdateProgressInfo();
}

public sealed class ProgressAggregatorTests
{
    [Fact]
    public void ProcessSetPrg_NullValue_WithMaximum_ShouldNotLogError()
    {
        using var sut = new TestableProgressAggregator();

        sut.PrgSet(null, 100, Abstractions.Enums.ProgressChangeMode.Set);

        sut.LoggedErrors.ShouldBeEmpty();
        sut.Maximum.ShouldBe(100);
    }

    [Fact]
    public void ProcessAddPrg_NullValue_WithMaximum_ShouldNotLogError()
    {
        using var sut = new TestableProgressAggregator();

        sut.PrgSetMax(50);
        sut.PrgMaxAdd(50);

        sut.LoggedErrors.ShouldBeEmpty();
        sut.Maximum.ShouldBe(100);
    }

    [Fact]
    public void ProcessSetPrg_ValueExceedsMaximum_ShouldLogError()
    {
        using var sut = new TestableProgressAggregator();

        sut.PrgSetMax(100);
        sut.PrgSet(200, null, Abstractions.Enums.ProgressChangeMode.Set);

        sut.LoggedErrors.Count.ShouldBe(1);
        sut.LoggedErrors[0].ShouldContain("out of range");
    }

    [Fact]
    public void ProcessAddPrg_ValueExceedsMaximum_ShouldLogError()
    {
        using var sut = new TestableProgressAggregator();

        sut.PrgSetMax(10);
        sut.PrgAdd(5);
        sut.PrgAdd(20);

        sut.LoggedErrors.Count.ShouldBe(1);
        sut.LoggedErrors[0].ShouldContain("exceeds maximum");
    }

    [Fact]
    public void Dispose_ShouldStopTimer()
    {
        var sut = new TestableProgressAggregator();
        sut.PrgSetMax(100);

        sut.Timer.IsRunning.ShouldBeTrue();

#pragma warning disable IDISP016, IDISP017 // intentional post-dispose assertion in test
        sut.Dispose();
        sut.Timer.IsRunning.ShouldBeFalse();
#pragma warning restore IDISP016, IDISP017
    }

    [Fact]
    public void Dispose_ShouldStopStopwatch()
    {
        var sut = new TestableProgressAggregator();
        sut.Stopwatch.Start();

        sut.Stopwatch.IsRunning.ShouldBeTrue();

#pragma warning disable IDISP016, IDISP017 // intentional post-dispose assertion in test
        sut.Dispose();
        sut.Stopwatch.IsRunning.ShouldBeFalse();
#pragma warning restore IDISP016, IDISP017
    }

    [Fact]
    public void UpdateProgressInfo_ValueIsZero_ShouldNotThrow()
    {
        using var sut = new TestableProgressAggregator();

        sut.PrgSet(0, 100, Abstractions.Enums.ProgressChangeMode.Set);
        sut.Stopwatch.Start();

        Should.NotThrow(() => sut.InvokeUpdateProgressInfo());
    }
}
