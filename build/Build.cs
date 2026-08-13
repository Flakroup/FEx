using FEx.Building;
using JetBrains.Annotations;
using Nuke.Common;
using System.Collections.Generic;

[DisableDefaultOutput(DefaultOutput.ErrorsAndWarnings)]
class Build : FExBuild, ITagTarget, ITestTarget
{
    // FEx ships packages, not deployable applications - nothing here to PublishApp.
    public override IEnumerable<string> PublishProjects => [];

    // No Info target: FExBuild logs the banner and parameter listing from OnBuildInitialized, so a target
    // doing the same printed all of it twice on every build - Info was DependentFor(Compile), not opt-in.

    [UsedImplicitly] // NUKE Target invoked by the build runner via reflection; R# cannot track it.
    Target Clean =>
        _ => _
            .Before(((ICompileTarget)this).Restore)
            .OnlyWhenStatic(() => !IsServerBuild)
            .Executes(() =>
            {
            });

    // Gates Publish on Test passing - one `Publish` invocation runs
    // Restore -> Compile -> Test -> Pack -> Publish -> Tag in a single process,
    // instead of CI needing separate test/publish jobs with their own checkout+restore.
    // A new pass-through target (not an override of Test/Publish - overriding either
    // would replace its Executes body wholesale and silently drop it from the plan).
    [UsedImplicitly] // NUKE Target invoked by the build runner via reflection; R# cannot track it.
    Target Verify =>
        _ => _
            .DependsOn(((ITestTarget)this).Test)
            .DependentFor(((INuGetPublishTarget)this).Publish);

    public static int Main()
    {
        Bootstrap();

        return Execute<Build>(x => ((ICompileTarget)x).Compile);
    }
}