using Nuke.Common.Tools.DotNet;

namespace FEx.Building;

public class RepositoryInfo : IRepositoryInfo
{
    public string CommitHash { get; }
    public string BranchName { get; }
    public string Type { get; }
    public string Url { get; }

    public RepositoryInfo(GitVersionInfo versionInfo, string repositoryUrl)
    {
        CommitHash = versionInfo.Sha;
        BranchName = versionInfo.BranchName;
        Type = "git";
        Url = repositoryUrl;
    }

    public RepositoryInfo(string commitHash, string branchName, string repositoryUrl, string type = "git")
    {
        CommitHash = commitHash;
        BranchName = branchName;
        Type = type;
        Url = repositoryUrl;
    }

    public DotNetPackSettings UpdateRepositoryInfo(DotNetPackSettings settings) =>
        settings.AddProperty("CommitHash", CommitHash)
            .AddProperty("RepositoryBranch", BranchName)
            .AddProperty("RepositoryType", Type)
            .AddProperty("RepositoryUrl", Url);
}