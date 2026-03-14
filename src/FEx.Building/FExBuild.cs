using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;

namespace FEx.Building;

public abstract class FExBuild : NukeBuild, ICompileTarget
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public virtual Configuration Configuration { get; } = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    public virtual Solution Solution { get; } = null!;

    public static IEnumerable<string> Logo { get; } =
    [
        " ███████████ ██████████             ███████████              ███  ████       █████ ",
        "░░███░░░░░░█░░███░░░░░█            ░░███░░░░░███            ░░░  ░░███      ░░███  ",
        " ░███   █ ░  ░███  █ ░  █████ █████ ░███    ░███ █████ ████ ████  ░███    ███████  ",
        " ░███████    ░██████   ░░███ ░░███  ░██████████ ░░███ ░███ ░░███  ░███   ███░░███  ",
        " ░███░░░█    ░███░░█    ░░░█████░   ░███░░░░░███ ░███ ░███  ░███  ░███  ░███ ░███  ",
        " ░███  ░     ░███ ░   █  ███░░░███  ░███    ░███ ░███ ░███  ░███  ░███  ░███ ░███  ",
        " █████       ██████████ █████ █████ ███████████  ░░████████ █████ █████ ░░████████ ",
        "░░░░░       ░░░░░░░░░░ ░░░░░ ░░░░░ ░░░░░░░░░░░    ░░░░░░░░ ░░░░░ ░░░░░   ░░░░░░░░  "];

    public virtual DotNetBuildSettings GetBuildSettings(
        DotNetBuildSettings settings,
        AbsolutePath solution,
        bool noRestore = true,
        DotNetVerbosity? verbosity = null) =>
        settings
            .SetConfiguration(Configuration)
            .SetNoRestore(noRestore)
            .SetProjectFile(solution)
            .SetProcessAdditionalArguments("-m", "-bl")
            .When(_ => !noRestore, s => s.SetProperty("NuGetAudit", !IsServerBuild))
            .When(_ => verbosity is not null, s => s.SetVerbosity(verbosity));

    public virtual DotNetRestoreSettings GetRestoreSettings(
        DotNetRestoreSettings settings,
        AbsolutePath solution,
        Configuration? configuration = null) =>
        settings
            .SetProjectFile(solution)
            .SetProperty("NuGetAudit", !IsServerBuild)
            .When(_ => configuration is not null, s => s.SetProperty("Configuration", configuration!.ToString()));

    public string GetBuildPlan() =>
        string.Join(" => ", ExecutionPlan
            .Where(static t => t.Status is ExecutionStatus.Scheduled)
            .Select(static target => target.Name));

    protected virtual void LogBuildInfo()
    {
        Log.Information("═══════════════════════════════════════════════════════════════");
        Log.Information("Command Line: {CommandLine}", Environment.CommandLine);
        Log.Information("Arguments:    {Args}", string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
        Log.Information("═══════════════════════════════════════════════════════════════");
        Log.Information("Build Parameters:");
        Log.Information("  Configuration: {Configuration}", Configuration);
        Log.Information("  Solution:      {Solution}", Solution);
        Log.Information("  IsServerBuild: {IsServerBuild}", IsServerBuild);
        Log.Information("═══════════════════════════════════════════════════════════════");
        Log.Information("Build Plan");
        Log.Information(GetBuildPlan());
    }

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
}
