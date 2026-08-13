using System.ComponentModel;

namespace FEx.AzureDevOpsx;

public enum TfsBuildServerVersion
{
    [Description("No TFS version detected")]
    None,

    [Description("2005")]
    V1,

    [Description("2008")]
    V2,

    [Description("2010")]
    V3,

    [Description("2012")]
    V4,

    [Description("2015")]
    V5
}