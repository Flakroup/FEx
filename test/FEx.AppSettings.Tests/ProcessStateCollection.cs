using Xunit;

namespace FEx.AppSettings.Tests;

/// <summary>
/// Classes that touch process-wide state - <c>ConfigurationManager</c>, the current directory. xUnit runs
/// classes in parallel, so they run one at a time through this collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessStateCollection
{
    public const string Name = "FEx.AppSettings process state";
}
