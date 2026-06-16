using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface ICompileTarget : INukeBuild
{
    Solution Solution { get; }
    Configuration Configuration { get; }

    Target Restore =>
        _ => _.Executes(() =>
        {
            DotNetRestore(s => s.SetProjectFile(Solution).SetProperty("NuGetAudit", !NukeBuild.IsServerBuild));
        });

    Target Compile =>
        _ => _.DependsOn(Restore)
            .Executes(() => { DotNetBuild(s => s.SetProjectFile(Solution).SetConfiguration(Configuration)); });
}