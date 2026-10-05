using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FEx.Building;

/// <summary>
/// One <c>dotnet</c> invocation as a build composes it: what to run and for how long it may run.
/// </summary>
/// <remarks>
/// The seam between a target and the process it starts, in the same spirit as <c>TestCommandLine</c>: a test
/// reaches the invocation a target would start, including its timeout, without starting anything.
/// </remarks>
/// <param name="Arguments">The command line after <c>dotnet</c>.</param>
/// <param name="Timeout">The bound, already validated; null for unbounded.</param>
/// <param name="WorkingDirectory">Where to start, or null for NUKE's default.</param>
/// <param name="Environment">Extra environment variables, or null for none.</param>
public sealed record DotNetInvocation(string Arguments,
                                      TimeSpan? Timeout,
                                      string? WorkingDirectory = null,
                                      IReadOnlyDictionary<string, string>? Environment = null)
{
    /// <summary>Starts the invocation; its whole process tree is what a timeout takes down.</summary>
    public IProcess Start() =>
        new ProcessTree(ProcessTasks.StartProcess(DotNetTasks.DotNetPath,
            Arguments,
            WorkingDirectory,
            Environment))!;
}

/// <summary>
/// Runs a process under a timeout that kills its whole tree.
/// </summary>
/// <remarks>
/// NUKE's own <c>timeout</c> argument stops at the root: <see cref="IProcess.Kill" /> reaches only the
/// <c>dotnet</c> that was started, and the test host, MSBuild nodes and compiler server it spawned go on
/// holding the binaries - the wedge the timeout exists to remove. See <see cref="ProcessTree" />.
/// </remarks>
public static class BoundedProcess
{
    /// <summary>
    /// The timeout as a build may state it: null and <see cref="Timeout.InfiniteTimeSpan" /> both mean
    /// unbounded.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Zero, any other negative value, or more than the wait can take (<see cref="int.MaxValue" />
    /// milliseconds). Such a value is a mistake, and clamping it would turn it into a build that dies at once.
    /// </exception>
    public static TimeSpan? Validate(TimeSpan? timeout, string name)
    {
        if (timeout is null || timeout == Timeout.InfiniteTimeSpan)
            return null;

        if (timeout <= TimeSpan.Zero || timeout.Value.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(name,
                timeout,
                $"{name} must be positive and at most {int.MaxValue} ms, or null / Timeout.InfiniteTimeSpan for no timeout.");

        return timeout;
    }

    /// <summary>Starts <paramref name="invocation" /> and waits for it under its timeout.</summary>
    /// <exception cref="TimeoutException">The timeout fired; the process tree has been killed.</exception>
    public static IReadOnlyCollection<Output> Run(DotNetInvocation invocation, string what) =>
        Run(invocation.Start, invocation.Timeout, what);

    /// <inheritdoc cref="Run(DotNetInvocation, string)" />
    /// <param name="start">Starts the process; hand it a <see cref="ProcessTree" /> for a real one.</param>
    /// <param name="timeout">The validated bound; null waits without limit.</param>
    /// <param name="what">Names the run in the failure message.</param>
    public static IReadOnlyCollection<Output> Run(Func<IProcess> start, TimeSpan? timeout, string what)
    {
        using var process = start();
        using var exited = new ManualResetEventSlim();

        // A thread of its own rather than a pool task: the wait is blocking by nature, and a build that runs
        // several of these must not starve the pool of the threads that would let them finish.
        new Thread(() =>
        {
            process.WaitForExit();
            exited.Set();
        }) { IsBackground = true, Name = "BoundedProcess.WaitForExit" }.Start();

        if (timeout is { } limit && !exited.Wait(limit) && TryKill(process))
        {
            exited.Wait();

            throw new TimeoutException(
                $"{what} did not finish within {limit}; its process tree was killed. Raise the timeout if the "
                + "run is legitimately that long, otherwise something in it is hung.");
        }

        exited.Wait();

        return process.AssertZeroExitCode().Output;
    }

    /// <summary>
    /// The command line NUKE would start for <paramref name="options" />, verb included. NUKE keeps the
    /// renderer internal and offers no way to start a settings object under a tree-killing wait, so the build
    /// path renders the arguments itself and starts the process on its own.
    /// </summary>
    /// <remarks>
    /// Unsafe accessors resolve at run time, so a NUKE release that renames the member fails the tests that
    /// render real settings rather than a build in the field.
    /// </remarks>
    public static string Render(ToolOptions options) => string.Join(' ', GetArguments(options));

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetArguments")]
    private static extern IEnumerable<string> GetArguments(ToolOptions options);

    // False when the process finished between the deadline and the kill: nothing timed out then.
    private static bool TryKill(IProcess process)
    {
        try
        {
            process.Kill();

            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
