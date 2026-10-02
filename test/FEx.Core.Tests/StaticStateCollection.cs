using Xunit;

namespace FEx.Core.Tests;

/// <summary>
/// Tests that touch process-wide state such as the logger behind <c>FExStaticLogger</c>. xUnit runs classes in
/// parallel, so two of them swapping that state at once would each see the other's value. One collection runs
/// them one at a time.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class StaticStateCollection
{
    public const string Name = "FEx.Core static state";
}
