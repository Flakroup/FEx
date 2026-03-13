using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface ITestTarget : INukeBuild
{
    sealed AbsolutePath TestResultsDirectory => NukeBuild.RootDirectory / "artifacts" / "test-results";

    Target Test => _ => _
        .Description("Runs tests with TRX logger")
        .Executes(() =>
        {
            TestResultsDirectory.CreateOrCleanDirectory();

            DotNetTest(s => s
                .SetProjectFile(((FExBuild)this).Solution)
                .SetConfiguration(((FExBuild)this).Configuration)
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("trx"));
        });
}
