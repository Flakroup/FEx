using Xunit;

namespace FEx.Asyncx.Tests;

/// <summary>
/// Tests that measure the CPU time of the whole test process. xUnit runs classes in parallel, so any other test
/// running at the same time would count as the measured code's CPU. A collection that disables parallelization
/// runs on its own, after the parallel collections have finished.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NonParallelCollection
{
    public const string Name = "FEx.Asyncx process CPU";
}
