using Shouldly;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The membership of <see cref="GlobalLoggerCollection" /> is what keeps the capturing sinks of the other
/// members free of foreign lines. Dropping the attribute from one of these compiles and passes - until a
/// run where the class logs while a sink is installed, and a replayed line goes missing.
/// </summary>
public sealed class GlobalLoggerCollectionTests
{
    public static TheoryData<Type> Writers() =>
    [
        typeof(InspectionRunTests),
        typeof(LogRedactionTests),
        typeof(SecretMaskingTests),
        // Run a real `dotnet` whose output reaches Log.Logger from thread-pool threads.
        typeof(BoundedProcessTests),
        // Report through Log.Information on the test's own thread.
        typeof(TagTargetTests),
        typeof(InspectTargetTests),
    ];

    [Theory]
    [MemberData(nameof(Writers))]
    public void AClassThatTouchesTheGlobalLogger_RunsInTheCollection(Type type) =>
        type.GetCustomAttributes<CollectionAttribute>()
            .Select(static attribute => attribute.Name)
            .ShouldContain(GlobalLoggerCollection.Name);
}
