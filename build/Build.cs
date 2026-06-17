using FEx.Building;
using JetBrains.Annotations;
using Nuke.Common;

[DisableDefaultOutput(DefaultOutput.ErrorsAndWarnings)]
class Build : FExBuild, ITagTarget, ITestTarget
{
    [UsedImplicitly] // NUKE Target invoked by the build runner via reflection; R# cannot track it.
    Target Info =>
        _ => _
            .DependentFor(((ICompileTarget)this).Compile)
            .Before(((ICompileTarget)this).Restore)
            .Executes(LogBuildInfo);

    [UsedImplicitly] // NUKE Target invoked by the build runner via reflection; R# cannot track it.
    Target Clean =>
        _ => _
            .Before(((ICompileTarget)this).Restore)
            .OnlyWhenStatic(() => !IsServerBuild)
            .Executes(() =>
            {
            });

    public static int Main()
    {
        Bootstrap();

        return Execute<Build>(x => ((ICompileTarget)x).Compile);
    }
}