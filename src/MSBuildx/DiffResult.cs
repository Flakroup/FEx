using FEx.Agnostics.Helpers;
using System.IO;

namespace FEx.MSBuildx;

public class DiffResult
{
    public FileInfo? Info { get; }
    public string? RelativePath { get; }
    public string? FullPath { get; }

    public MSProject Project { get; }

    public string ItemType => Item?.ItemType ?? "Ignored";

    public bool Exists => Info?.Exists == true;

    public long Length =>
        Exists
            ? Info!.Length // Exists is true only when Info is non-null
            : 0L;

    public string? Extension => Info?.Extension;

    private MSProjectItem? Item { get; }

    public DiffResult(MSProject project, string? relativePath = null, MSProjectItem? item = null)
    {
        Project = project;

        if (item is not null)
        {
            Item = item;
            RelativePath = Item.EvaluatedInclude;
        }
        else
        {
            RelativePath = relativePath;
        }

        if (RelativePath is not null && !RelativePath.PathHasIllegalCharacters())
            FullPath = Path.Combine(Project.ProjectDir, RelativePath);

        if (FullPath is not null)
            Info = new(FullPath);
    }
}