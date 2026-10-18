using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace FEx.Building;

/// <summary>
/// What one thread cap turns into: the MSBuild switch, the environment every child process inherits and the
/// <c>dotnet test</c> options. Pure, so each translation is testable without a build.
/// </summary>
/// <remarks>
/// A cap has to reach four consumers separately, because none of them reads the others' setting:
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
/// each cap its own server, started with the capped environment. It REPLACES Roslyn's own pipe name, which
/// carries the compiler's directory, so the id carries the repository instead: one repository resolves one
/// SDK through its <c>global.json</c>, and a server of another SDK answers a mismatched client with an
/// error that fails the build.
/// </item>
/// <item>
/// The test runner. MTP runs one test module per core and xUnit v3 one thread per core by default, hence the
/// two explicit options.
/// </item>
/// <item>
/// The machine as a whole. Every number above sizes ONE process, and a build runs many at once, so together
/// they still fill every core - measured on a 32-thread machine, a cap of 24 kept all 32 busy at p95. What
/// confines the whole tree is the processor affinity (<see cref="AffinityMask" />), which the build sets on
/// itself and every child process inherits. It does not replace the counts: MSBuild sizes its node pool by
/// the cores the OS reports, ignoring the affinity, and oversubscribing the allowed cores only adds
/// switching. MSBuild's worker nodes outlive the build and keep the affinity of the build that started them,
/// and their handshake checks neither affinity nor environment, so <c>MSBUILDNODEHANDSHAKESALT</c> gives a
/// capped build nodes of its own, as <c>SharedCompilationId</c> does the compiler server. Neither is reached
/// by <c>dotnet build-server shutdown</c>, which stops the default-named servers only.
/// </item>
/// </list>
/// A null cap means unbounded and produces nothing at all, so a server build's command lines, environment and
/// affinity stay exactly what they were before the cap existed.
/// </remarks>
public static class Parallelism
{
    private const string ProcessorCountVariable = "DOTNET_PROCESSOR_COUNT";
    private const string CompilationIdVariable = "SharedCompilationId";
    private const string NodeSaltVariable = "MSBUILDNODEHANDSHAKESALT";
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

    /// <summary>
    /// The environment variables the cap sets for every child process; empty without a cap. The compiler server
    /// id is scoped to <paramref name="root" />, the repository being built.
    /// </summary>
    /// <remarks>
    /// ponytail: an SDK installed while a capped server is still warm fails ONE build of that repository with
    /// the hash mismatch, and the mismatched server shuts itself down; keying on the resolved SDK directory
    /// would close that, at the cost of resolving it.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> Environment(int? cap, string root)
    {
        if (cap is not { } n)
            return new Dictionary<string, string>();

        var id = $"fex-p{n.ToString(CultureInfo.InvariantCulture)}-{Scope(root)}";

        return new Dictionary<string, string>
        {
            [ProcessorCountVariable] = n.ToString(CultureInfo.InvariantCulture),
            [CompilationIdVariable] = id,
            [NodeSaltVariable] = id
        };
    }

    private static string Scope(string root) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(root.ToUpperInvariant())))[..8];

    /// <summary>
    /// The processor affinity that confines the build to <paramref name="cap" /> of the processors
    /// <paramref name="current" /> allows - the highest ones, leaving the lowest, processor 0 included, to the
    /// rest of the machine. Null means leave the affinity alone: no cap, or a mask that already allows no more
    /// processors than the cap.
    /// </summary>
    /// <remarks>
    /// Narrows the CURRENT mask rather than building one from the processor count, so a build already confined
    /// by its caller - a container, a job object, a parent with its own affinity - is never widened.
    /// </remarks>
    public static long? AffinityMask(int? cap, long current)
    {
        if (cap is not { } remaining || remaining <= 0 || BitOperations.PopCount((ulong)current) <= remaining)
            return null;

        var mask = 0L;

        for (var bit = 63; remaining > 0; bit--)
        {
            var processor = 1L << bit;

            if ((current & processor) == 0)
                continue;

            mask |= processor;
            remaining--;
        }

        return mask;
    }

    /// <summary>
    /// The <c>dotnet test</c> options for the cap. An option pair is left out when
    /// <paramref name="alreadyPresent" /> names it, in any form MTP parses (<c>--opt v</c>, <c>--opt:v</c>,
    /// <c>--opt=v</c>): MTP sums the arity of a repeated option and aborts the run, so a repository that sets
    /// its own value wins. xUnit applies the command line after <c>xunit.runner.json</c>, so a project that limits
    /// <c>maxParallelThreads</c> there gets the cap instead; such a limit belongs in the arguments.
    /// </summary>
    public static IReadOnlyList<string> TestArguments(int? cap, IEnumerable<string>? alreadyPresent)
    {
        if (cap is not { } n)
            return [];

        var present = (alreadyPresent ?? []).ToList();
        var value = n.ToString(CultureInfo.InvariantCulture);
        List<string> arguments = [];

        if (!Carries(present, ModulesOption))
            arguments.AddRange([ModulesOption, value]);

        if (!Carries(present, ThreadsOption))
            arguments.AddRange([ThreadsOption, value]);

        return arguments;
    }

    private static bool Carries(IEnumerable<string> arguments, string option) =>
        arguments.Any(argument => argument == option ||
                                  argument.StartsWith($"{option}:", StringComparison.Ordinal) ||
                                  argument.StartsWith($"{option}=", StringComparison.Ordinal));
}
