namespace FEx.McpServer.Services;

public enum ApiEntryType
{
    Class,
    Interface,
    Enum,
    Extension
}

public sealed class ApiEntry
{
    public ApiEntryType Type { get; init; }
    public string Name { get; init; } = "";
    public string Namespace { get; init; } = "";
    public string Project { get; init; } = "";
    public string File { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Signature { get; init; } = "";
    public string Returns { get; init; } = "";
    public string Base { get; init; } = "";
    public bool IsStatic { get; init; }
}

public sealed class ProjectInfo
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public int Classes { get; init; }
    public int Interfaces { get; init; }
    public int Enums { get; init; }
    public int Extensions { get; init; }
}

public sealed class ApiSurfaceConfig
{
    public string ApiSurfacePath { get; }
    public string RepoPath { get; }

    public ApiSurfaceConfig(string apiSurfacePath, string repoPath)
    {
        ApiSurfacePath = apiSurfacePath;
        RepoPath = repoPath;
    }
}
