using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface ITestTarget : ICompileTarget
{
    sealed AbsolutePath TestResultsDirectory => NukeBuild.RootDirectory / "artifacts" / "test-results";

    Target Test =>
        _ => _.Description("Runs tests with TRX logger")
            .DependsOn(Compile)
            .Executes(() =>
            {
                TestResultsDirectory.CreateOrCleanDirectory();

                DotNetTest(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .SetResultsDirectory(TestResultsDirectory)
                    .SetLoggers("trx"));
            });
}