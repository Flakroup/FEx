using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces.Collections;
using Microsoft.Build.Evaluation;
using System.Diagnostics;
using System.IO;

namespace FEx.MSBuildx;

[DebuggerDisplay("{ItemType}={EvaluatedInclude} [{UnevaluatedInclude}] #DirectMetadata={DirectMetadataCount}")]
public class MSProjectItem
{
    public static IMap<string, ProjectItemType> ProjectItemTypes { get; } =
        EnumExtensions.GetEnumMap<ProjectItemType>(true);

    public ProjectItem ProjectItem { get; }
    public string FullPath { get; }
    public string Extension { get; }
    public ProjectItemType? ProjectItemType { get; }
    public string ItemType => ProjectItem.ItemType;
    public string EvaluatedInclude => ProjectItem.EvaluatedInclude;
    public string UnevaluatedInclude => ProjectItem.UnevaluatedInclude;
    public int DirectMetadataCount => ProjectItem.DirectMetadataCount;

    public MSProjectItem(ProjectItem projectItem, MSProject project)
    {
        ProjectItem = projectItem;
        FullPath = Path.Combine(project.ProjectDir, ProjectItem.EvaluatedInclude);
        Extension = Path.GetExtension(FullPath);
        ProjectItemType = GetProjectItemType();
    }

    private ProjectItemType? GetProjectItemType() =>
        ProjectItemTypes.ForwardIndex.TryGetValue(ItemType, out ProjectItemType res)
            ? res
            : null;
}
