using FEx.Building;
using Nuke.Common;

[DisableDefaultOutput(DefaultOutput.ErrorsAndWarnings)]
class Build : FExBuild, ITagTarget, ITestTarget
{
    public static int Main()
    {
        Bootstrap();
        return Execute<Build>(x => ((ICompileTarget)x).Compile);
    }

    Target Info => _ => _
        .DependentFor(((ICompileTarget)this).Compile)
        .Before(((ICompileTarget)this).Restore)
        .Executes(LogBuildInfo);

    Target Clean => _ => _
        .Before(((ICompileTarget)this).Restore)
        .OnlyWhenStatic(() => !IsServerBuild)
        .Executes(() => { });
}
