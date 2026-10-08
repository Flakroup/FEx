using Xunit;

namespace FEx.Building.Tests;

/// <summary>
/// The classes that swap Serilog's process-wide <c>Log.Logger</c> for a capturing sink, and the classes that
/// WRITE to it. xUnit runs classes in parallel, so two swappers at once each restore the other's "previous"
/// logger and capture nothing; and a class that logs while another holds a sink puts its lines into that sink
/// from its own threads - the sinks keep their messages in a plain <c>List</c>, so a concurrent add loses one
/// (measured: <c>dotnet no-such-dotnet-verb</c> in <c>BoundedProcessTests</c> writes its output from thread-pool
/// threads, and the replay test of <c>InspectionRunTests</c> lost its first line to it). Red on a fast
/// many-core machine, green on a slower CI runner. One collection runs them one at a time.
/// </summary>
/// <remarks>
/// A class that starts logging through <c>Log</c> (a real child process, a target that reports) joins the
/// collection; <c>GlobalLoggerCollectionTests</c> pins the ones known to.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class GlobalLoggerCollection
{
    public const string Name = "Serilog Log.Logger";
}
