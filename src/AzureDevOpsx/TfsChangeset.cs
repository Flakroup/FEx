using System;

namespace FEx.AzureDevOpsx;

public class TfsChangeset
{
    public string ChangesetId { get; set; }
    public string Committer { get; set; }
    public string CommitterDisplayName { get; set; }
    public DateTime CreationDate { get; set; }
    public Uri ChangesetUrl { get; set; }

    public TfsChangeset()
    {
        ChangesetId = string.Empty;
        Committer = string.Empty;
        CommitterDisplayName = string.Empty;
        CreationDate = DateTime.MinValue;
    }
}
