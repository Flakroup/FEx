using Xunit;

namespace FEx.Json.Tests;

/// <summary>
/// The classes that swap the logger behind <c>FExStaticLogger</c>. xUnit runs classes in parallel, so another
/// class logging a failed <c>FromJson</c> at the same time would land in the swapped logger. A collection that
/// disables parallelization runs them on their own.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class StaticStateCollection
{
    public const string Name = "FEx.Json static state";
}
