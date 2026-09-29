using Xunit;

namespace FEx.AspNetCorex.Tests;

/// <summary>
/// The classes that exercise <see cref="IdempotencyMiddleware"/>'s static in-flight lock dictionary. xUnit
/// runs classes in parallel, so a count check in one class can observe an entry another class added or
/// removed at the same moment - a random failure on a fast many-core machine, green on a slower runner.
/// One collection runs them one at a time.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IdempotencyLockCollection
{
    public const string Name = "IdempotencyMiddleware in-flight lock";
}
