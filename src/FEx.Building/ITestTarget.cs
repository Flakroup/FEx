using Nuke.Common;
using Nuke.Common.IO;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace FEx.Building;

public interface ITestTarget : ICompileTarget
{
    sealed AbsolutePath TestResultsDirectory => NukeBuild.RootDirectory / "artifacts" / "test-results";

    Target Test =>
        _ => _.Description("Runs tests via Microsoft.Testing.Platform (MTP)")
            .DependsOn(Compile)
            .Executes(() =>
            {
                TestResultsDirectory.CreateOrCleanDirectory();

                // MTP needs `--solution` for a .slnx and `--report-xunit-trx`; it rejects the VSTest
                // `--logger trx`. Test projects build as Exe with UseMicrosoftTestingPlatformRunner.
                DotNet($"test --solution {Solution} --configuration {Configuration} --no-build " +
                       $"--results-directory {TestResultsDirectory} --report-xunit-trx");
            });
}