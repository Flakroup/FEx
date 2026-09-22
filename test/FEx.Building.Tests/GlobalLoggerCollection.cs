using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The classes that swap Serilog's process-wide <c>Log.Logger</c> for a capturing sink. xUnit runs classes in
/// parallel, so two of them swapping at once each restore the other's "previous" logger and capture nothing -
/// red on a fast many-core machine, green on a slower CI runner. One collection runs them one at a time.
/// </summary>
[CollectionDefinition(Name)]
public sealed class GlobalLoggerCollection
{
    public const string Name = "Serilog Log.Logger";
}
