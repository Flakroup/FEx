using FEx.Building;
using JetBrains.Annotations;
using Nuke.Common;
using System.Collections.Generic;

[DisableDefaultOutput(DefaultOutput.ErrorsAndWarnings)]
class Build : FExBuild, ICoverageTarget, IInspectTarget, ITagTarget
{
    // FEx ships packages, not deployable applications - nothing here to PublishApp.
    public override IEnumerable<string> PublishProjects => [];

    // The ratchet: every new file is held to 100%; files below it today are listed one by one in
    // CoverageDebt, which only shrinks. See build/CoverageDebt.cs.
    public IReadOnlyList<string> CoverageExclusions => CoverageDebt.Files;

    public IReadOnlyList<string> ModulesWithoutExecutableCode => CoverageDebt.ModulesWithoutExecutableCode;

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

    // Gates Publish on Coverage (which runs Test) passing - one `Publish` invocation runs
    // Restore -> Compile -> Test -> Coverage -> Pack -> Publish -> Tag in a single process.
    // CI splits that chain across two jobs (build: `Coverage Inspect Pack`, publish: `Publish` with the
    // earlier targets skipped over the uploaded packages), so the gate is the build job's Coverage there.
    // A new pass-through target (not an override of Test/Publish - overriding either
    // would replace its Executes body wholesale and silently drop it from the plan).
    [UsedImplicitly] // NUKE Target invoked by the build runner via reflection; R# cannot track it.
    Target Verify =>
        _ => _
            .DependsOn(((ICoverageTarget)this).Coverage)
            .DependentFor(((INuGetPublishTarget)this).Publish);

    public static int Main()
    {
        Bootstrap();

        return Execute<Build>(x => ((ICompileTarget)x).Compile);
    }
}