using Nuke.Common.Tools.DotNet;

namespace FEx.Building;

public interface IRepositoryInfo
{
    string CommitHash { get; }
    string BranchName { get; }
    string Type { get; }
    string Url { get; }

    DotNetPackSettings UpdateRepositoryInfo(DotNetPackSettings settings);
}
