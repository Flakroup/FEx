using FEx.Building;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : FExBuild, ITagTarget
{
    AbsolutePath TestResultsDirectory => RootDirectory / "artifacts" / "test-results";

    public static int Main()
    {
        Bootstrap();
        return Execute<Build>(x => x.Compile);
    }

    Target Info => _ => _
        .DependentFor(Clean, Restore, Compile)
        .Executes(LogBuildInfo);

    Target Clean => _ => _
        .Before(Restore)
        .OnlyWhenStatic(() => !IsServerBuild)
        .Executes(() => { });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => GetRestoreSettings(s, Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => GetBuildSettings(s, Solution));
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoBuild(true)
                .SetResultsDirectory(TestResultsDirectory)
                .AddLoggers("trx"));
        });
}
