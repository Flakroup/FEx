using Nuke.Common.Tools.DotNet;

namespace FEx.Building;

public static class NukeExtensions
{
    public static DotNetPackSettings UpdateRepositoryInfo(this DotNetPackSettings toolSettings,
                                                          IRepositoryInfo repositoryInfo) =>
        repositoryInfo.UpdateRepositoryInfo(toolSettings);
}