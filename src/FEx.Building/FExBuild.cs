using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.ProjectModel;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FEx.Building;

public abstract class FExBuild : NukeBuild, IAppPublishTarget, ITestTarget
{
    public static IEnumerable<string> Logo { get; } =
    [
        " ███████████ ██████████             ███████████              ███  ████       █████ ",
        "░░███░░░░░░█░░███░░░░░█            ░░███░░░░░███            ░░░  ░░███      ░░███  ",
        " ░███   █ ░  ░███  █ ░  █████ █████ ░███    ░███ █████ ████ ████  ░███    ███████  ",
        " ░███████    ░██████   ░░███ ░░███  ░██████████ ░░███ ░███ ░░███  ░███   ███░░███  ",
        " ░███░░░█    ░███░░█    ░░░█████░   ░███░░░░░███ ░███ ░███  ░███  ░███  ░███ ░███  ",
        " ░███  ░     ░███ ░   █  ███░░░███  ░███    ░███ ░███ ░███  ░███  ░███  ░███ ░███  ",
        " █████       ██████████ █████ █████ ███████████  ░░████████ █████ █████ ░░████████ ",
        "░░░░░       ░░░░░░░░░░ ░░░░░ ░░░░░ ░░░░░░░░░░░    ░░░░░░░░ ░░░░░ ░░░░░   ░░░░░░░░  "
    ];

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public virtual Configuration Configuration { get; } = IsLocalBuild
        ? Configuration.Debug
        : Configuration.Release;

    /// <inheritdoc />
    public abstract IEnumerable<string> PublishProjects { get; }

    /// <inheritdoc />
    [Parameter("Runtime to publish for")]
    public string? PublishRuntime { get; }

    [Solution]
    public virtual Solution Solution { get; } = null!;

    /// <inheritdoc />
    [Parameter("Indicates whether to publish as self-contained")]
    public bool PublishSelfContained { get; }

    /// <inheritdoc />
    [Parameter("Indicates whether to publish as single-file")]
    public bool PublishSingleFile { get; }

    /// <inheritdoc />
    [Parameter("Framework to publish for")]
    public string? PublishFramework { get; }

    /// <summary>
    /// Returns a human-readable summary of the scheduled execution plan
    /// (e.g., "Clean => Restore => Compile").
    /// </summary>
    public string GetBuildPlan() =>
        string.Join(" => ",
            ExecutionPlan.Where(static t => t.Status is ExecutionStatus.Scheduled)
                .Select(static target => target.Name));

    protected static void PrintBanner()
    {
        foreach (var line in Logo)
            Console.WriteLine(line);

        Console.WriteLine();
    }

    protected static void DisableTelemetry()
    {
        Environment.SetEnvironmentVariable("NUKE_NOLOGO", "true");
        Environment.SetEnvironmentVariable("NUKE_TELEMETRY_OPTOUT", "1");
        Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
        Environment.SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
        Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0");
        Environment.SetEnvironmentVariable("DOTNET_NOLOGO", "1");
        Console.WriteLine("Disabled .NET and NUKE telemetry");
    }

    protected static void Bootstrap()
    {
        PrintBanner();
        DisableTelemetry();
    }

    protected virtual void LogBuildInfo()
    {
        Log.Information("═══════════════════════════════════════════════════════════════");
        Log.Information("Command Line: {CommandLine}", Environment.CommandLine);
        Log.Information("═══════════════════════════════════════════════════════════════");
        Log.Information("Build Parameters:");

        var entries = new List<(string Name, object? Value)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Add(nameof(Solution), Solution.Path);
        Add(nameof(IsServerBuild), IsServerBuild);

        entries.AddRange(GetParameterEntries(seen).OrderBy(static e => e.Name));

        var pad = entries.Max(static e => e.Name.Length) + 1;

        foreach (var (name, value) in entries)
            Log.Information("  {Name} {Value}", $"{name}:".PadRight(pad), FormatParameterValue(name, value));

        Log.Information("═══════════════════════════════════════════════════════════════");
        Log.Information("Build Plan");
        Log.Information(GetBuildPlan());

        return;

        void Add(string name, object? value)
        {
            if (seen.Add(name))
                entries.Add((name, value));
        }
    }

    /// <summary>
    /// Renders a parameter value for the <see cref="LogBuildInfo" /> output. The default
    /// implementation returns <c>"(not set)"</c> for null and a one-line summary for the
    /// few NUKE/FEx injection types whose <c>ToString()</c> dump would be too verbose
    /// (e.g. <see cref="GitVersionInfo" />). Override in derived builds to add component-specific
    /// formatting; call <c>base.FormatParameterValue</c> for the default fallback.
    /// </summary>
    protected virtual string FormatParameterValue(string name, object? value) =>
        value switch
        {
            null => "(not set)",
            GitVersionInfo gv => $"{gv.SemVer} ({gv.EscapedBranchName}@{gv.ShortSha})",
            _ => value.ToString() ?? "(not set)"
        };

    /// <inheritdoc />
    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();
        LogBuildInfo();
    }

    private IEnumerable<(string Name, object? Value)> GetParameterEntries(HashSet<string> seen)
    {
        var nukeAssembly = typeof(NukeBuild).Assembly;
        var type = GetType();

        const BindingFlags flags = BindingFlags.Public
                                   | BindingFlags.NonPublic
                                   | BindingFlags.Instance
                                   | BindingFlags.FlattenHierarchy;

        var properties = type.GetProperties(flags)
            .Concat(type.GetInterfaces().SelectMany(static i => i.GetProperties()));

        foreach (var prop in properties)
        {
            if (prop.GetCustomAttribute<ParameterAttribute>() is not { } attr)
                continue;

            // Skip NUKE's own infrastructure parameters (Help, NoLogo, Plan, Target, ...).
            if (prop.DeclaringType?.Assembly == nukeAssembly)
                continue;

            var name = attr.Name ?? prop.Name;

            if (!seen.Add(name))
                continue;

            object? value;

            try
            {
                value = prop.GetValue(this);
            }
            catch (Exception ex)
            {
                value = $"(error: {ex.GetBaseException().Message})";
            }

            yield return (name, value);
        }
    }

    /// <inheritdoc />
    [Parameter("Run only test classes matching this name - wildcards with '*' (e.g. '*OrderTests')")]
    public string? TestFilter { get; }
}