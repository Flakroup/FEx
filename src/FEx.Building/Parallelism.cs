using System;
using System.Collections.Generic;
using System.Globalization;

namespace FEx.Building;

/// <summary>
/// What one thread cap turns into: the MSBuild switch, the environment every child process inherits and the
/// <c>dotnet test</c> options. Pure, so each translation is testable without a build.
/// </summary>
/// <remarks>
/// A cap has to reach three consumers separately, because none of them reads the others' setting:
/// <list type="bullet">
/// <item>
/// MSBuild. A bare <c>-m</c> / <c>-maxcpucount</c> counts cores through
/// <c>NativeMethodsShared.GetLogicalCoreCount()</c>, which on Windows asks the OS directly and ignores
/// <c>DOTNET_PROCESSOR_COUNT</c>. Every MSBuild call therefore needs the explicit <c>-maxcpucount:N</c>; the
/// dotnet CLI itself prepends a bare <c>-maxcpucount</c> and MSBuild takes the LAST value given.
/// </item>
/// <item>
/// Roslyn. It sizes its parallelism from <c>Environment.ProcessorCount</c>, which
/// <c>DOTNET_PROCESSOR_COUNT</c> overrides. The shared compiler server (VBCSCompiler) is reused across builds
/// and its pipe name does not include the environment, so a server started uncapped would keep serving the
/// capped build. <c>SharedCompilationId</c>, which MSBuild reads from the environment as a property, gives
/// each cap its own server, started with the capped environment.
/// </item>
/// <item>
/// The test runner. MTP runs one test module per core and xUnit v3 one thread per core by default, hence the
/// two explicit options.
/// </item>
/// </list>
/// A null cap means unbounded and produces nothing at all, so a server build's command lines and environment
/// stay exactly what they were before the cap existed.
/// </remarks>
public static class Parallelism
{
    private const string ProcessorCountVariable = "DOTNET_PROCESSOR_COUNT";
    private const string CompilationIdVariable = "SharedCompilationId";
    private const string ModulesOption = "--max-parallel-test-modules";
    private const string ThreadsOption = "--max-threads";

    /// <summary>
    /// The local default cap for a machine with <paramref name="threads" /> hardware threads: a quarter of the
    /// machine, and never fewer than two threads, stay free for the person working on it.
    /// </summary>
    /// <remarks>
    /// 4 -> 2, 8 -> 6, 16 -> 12, 32 -> 24, 64 -> 48; a machine of one to three threads gets one.
    /// </remarks>
    public static int LocalDefault(int threads) => Math.Max(1, threads - Math.Max(2, threads / 4));

    /// <summary>
    /// Null for no cap (null or zero), the value for a positive one.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A negative value.</exception>
    public static int? Resolve(int? requested) =>
        requested switch
        {
            null or 0 => null,
            > 0 => requested,
            _ => throw new ArgumentOutOfRangeException("MaxParallelism",
                requested,
                "MaxParallelism must be zero (unbounded) or a positive number of threads.")
        };

    /// <summary>The MSBuild switch Build carries: <c>-m</c> without a cap, <c>-m:N</c> with one.</summary>
    public static string MsBuildSwitch(int? cap) =>
        cap is { } n
            ? $"-m:{n.ToString(CultureInfo.InvariantCulture)}"
            : "-m";

    /// <summary>
    /// What a command that carries no <c>-m</c> of its own (restore, pack, publish) adds: nothing without a
    /// cap, <c>-maxcpucount:N</c> with one.
    /// </summary>
    public static IReadOnlyList<string> MsBuildArguments(int? cap) =>
        cap is { } n
            ? [$"-maxcpucount:{n.ToString(CultureInfo.InvariantCulture)}"]
            : [];

    /// <summary>The environment variables the cap sets for every child process; empty without a cap.</summary>
    public static IReadOnlyDictionary<string, string> Environment(int? cap) =>
        cap is { } n
            ? new Dictionary<string, string>
            {
                [ProcessorCountVariable] = n.ToString(CultureInfo.InvariantCulture),
                [CompilationIdVariable] = $"fex-p{n.ToString(CultureInfo.InvariantCulture)}"
            }
            : new Dictionary<string, string>();

    /// <summary>
    /// The <c>dotnet test</c> options for the cap. An option pair is left out when
    /// <paramref name="alreadyPresent" /> names it: MTP sums the arity of a repeated option and aborts the
    /// run, so a repository that sets its own value wins.
    /// </summary>
    public static IReadOnlyList<string> TestArguments(int? cap, IEnumerable<string>? alreadyPresent)
    {
        if (cap is not { } n)
            return [];

        var present = new HashSet<string>(alreadyPresent ?? [], StringComparer.Ordinal);
        var value = n.ToString(CultureInfo.InvariantCulture);
        List<string> arguments = [];

        if (!present.Contains(ModulesOption))
            arguments.AddRange([ModulesOption, value]);

        if (!present.Contains(ThreadsOption))
            arguments.AddRange([ThreadsOption, value]);

        return arguments;
    }
}
