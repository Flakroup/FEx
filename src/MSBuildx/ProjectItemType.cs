using System.ComponentModel;

namespace FEx.MSBuildx;

public enum ProjectItemType
{
    [Description("None")]
    None,

    [Description("EmbeddedResource")]
    EmbeddedResource
}