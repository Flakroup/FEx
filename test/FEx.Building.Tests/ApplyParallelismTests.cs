using Serilog;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The one collection that writes the PROCESS environment, and so runs alone: every child process another
/// test starts inherits whatever is set here while it is set.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection
{
    public const string Name = "Process environment";
}

/// <summary>
/// A cap reaches the compiler and every .NET tool the build starts through the environment, because the
/// children read it themselves, and through the processor affinity, which they inherit. These pin that both
/// are set, that no cap sets nothing, and that the hook NUKE calls is what sets them.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ApplyParallelismTests : IDisposable
{
    private const string ProcessorCount = "DOTNET_PROCESSOR_COUNT";
    private const string CompilationId = "SharedCompilationId";
    private const string NodeSalt = "MSBUILDNODEHANDSHAKESALT";
    private const string Root = @"X:\Repo";

    private readonly string? _processorCount = Environment.GetEnvironmentVariable(ProcessorCount);
    private readonly string? _compilationId = Environment.GetEnvironmentVariable(CompilationId);
    private readonly string? _nodeSalt = Environment.GetEnvironmentVariable(NodeSalt);
    private readonly ILogger _logger = Log.Logger;
    private readonly long _affinity = Affinity;

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(ProcessorCount, _processorCount);
        Environment.SetEnvironmentVariable(CompilationId, _compilationId);
        Environment.SetEnvironmentVariable(NodeSalt, _nodeSalt);
        Log.Logger = _logger;
        Affinity = _affinity;
    }

    /// <summary>The test host's own affinity, which a cap narrows and <see cref="Dispose" /> restores.</summary>
    /// <remarks>CI and the machines this build runs on are Windows or Linux, the two that have one.</remarks>
    private static long Affinity
    {
        get
        {
            using var process = Process.GetCurrentProcess();

            return AffinityOf(process);
        }
        set
        {
            using var process = Process.GetCurrentProcess();

            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
                process.ProcessorAffinity = (nint)value;
            else
                throw new PlatformNotSupportedException();
        }
    }

    private static long AffinityOf(Process process)
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            return (long)process.ProcessorAffinity;

        throw new PlatformNotSupportedException();
    }

    [Fact]
    public void ACap_ConfinesTheBuildToThatManyProcessors()
    {
        var expected = Parallelism.AffinityMask(1, _affinity) ?? _affinity;

        CappedBuild.Apply(1);

        Affinity.ShouldBe(expected);
        BitOperations.PopCount((ulong)Affinity).ShouldBe(1);
    }

    [Fact]
    public void ACap_IsInheritedByTheProcessesTheBuildStarts()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(),
            "Linux binds the calling thread only, and a test runs off the main thread; see FExBuild.ApplyParallelism.");

        CappedBuild.Apply(1);

        // A constant command line, nothing from input: the child only has to live long enough to be read.
        using var child = Process.Start( // nosemgrep: csharp_injection_rule-CommandInjection
            new ProcessStartInfo("ping", "-n 30 127.0.0.1") { UseShellExecute = false, RedirectStandardOutput = true })!;

        try
        {
            AffinityOf(child).ShouldBe(Affinity);
        }
        finally
        {
            child.Kill();
            child.WaitForExit();
        }
    }

    [Fact]
    public void NoCap_LeavesTheAffinityAlone()
    {
        CappedBuild.Apply(null);

        Affinity.ShouldBe(_affinity);
    }

    [Fact]
    public void ACap_SetsTheProcessorCountAndItsOwnCompilerServer()
    {
        Environment.SetEnvironmentVariable(ProcessorCount, null);
        Environment.SetEnvironmentVariable(CompilationId, null);

        CappedBuild.Apply(6);

        Environment.GetEnvironmentVariable(ProcessorCount).ShouldBe("6");
        Environment.GetEnvironmentVariable(CompilationId).ShouldBe(Parallelism.Environment(6, Root)[CompilationId]);
        Environment.GetEnvironmentVariable(NodeSalt).ShouldBe(Parallelism.Environment(6, Root)[NodeSalt]);
    }

    [Fact]
    public void NoCap_LeavesWhatTheCallerExportedUntouched()
    {
        Environment.SetEnvironmentVariable(ProcessorCount, "3");
        Environment.SetEnvironmentVariable(CompilationId, "mine");

        CappedBuild.Apply(null);

        Environment.GetEnvironmentVariable(ProcessorCount).ShouldBe("3");
        Environment.GetEnvironmentVariable(CompilationId).ShouldBe("mine");
    }

    [Fact]
    public void OnBuildInitialized_AppliesTheCap()
    {
        Environment.SetEnvironmentVariable(ProcessorCount, null);
        Environment.SetEnvironmentVariable(CompilationId, null);

        try
        {
            new CappedBuild(6).Initialize();
        }
        catch (Exception)
        {
            // LogBuildInfo throws at NUKE's execution plan; the cap is applied before it.
        }

        Environment.GetEnvironmentVariable(ProcessorCount).ShouldBe("6");
        Environment.GetEnvironmentVariable(CompilationId).ShouldStartWith("fex-p6-");
    }

    [Fact]
    public void OnBuildInitialized_TreatsZeroAsUnbounded()
    {
        Environment.SetEnvironmentVariable(ProcessorCount, "3");
        Environment.SetEnvironmentVariable(CompilationId, "mine");

        try
        {
            new CappedBuild(0).Initialize();
        }
        catch (Exception)
        {
            // See OnBuildInitialized_AppliesTheCap.
        }

        Environment.GetEnvironmentVariable(ProcessorCount).ShouldBe("3");
        Environment.GetEnvironmentVariable(CompilationId).ShouldBe("mine");
    }

    [Fact]
    public void OnBuildInitialized_FailsTheBuildOnANegativeCap() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new CappedBuild(-1).Initialize())
            .ParamName.ShouldBe("MaxParallelism");

    private sealed class CappedBuild : FExBuild
    {
        private readonly int? _cap;

        public CappedBuild(int? cap) => _cap = cap;

        public override IEnumerable<string> PublishProjects { get; } = [];

        public override int? MaxParallelism => _cap;

        public static void Apply(int? cap) => ApplyParallelism(cap, Root);

        public void Initialize() => OnBuildInitialized();
    }
}
