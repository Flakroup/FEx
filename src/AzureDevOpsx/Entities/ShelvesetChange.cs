using FEx.Agnostics.Abstractions.Extensions;
using FEx.AzureDevOpsx.Extensions;
using Microsoft.TeamFoundation.VersionControl.Client;
using Newtonsoft.Json;
using System.Linq;

namespace FEx.AzureDevOpsx.Entities;

public class ShelvesetChange
{
    private ChangeType? _type;

    [JsonProperty("item")]
    public ChangeItem Item { get; set; }

    [JsonProperty("changeType")]
    public string ChangeTypeString { get; set; }

    [JsonProperty("sourceServerItem")]
    public string SourceServerItem { get; set; }

    [JsonIgnore]
    public ChangeType Type
    {
        get
        {
            if (_type == null)
            {
                _type = ChangeType.None;

                foreach (var changeType in ChangeTypeString.Split(',').Select(x => x.Trim()).ToArray())
                {
                    if (_type == ChangeType.None)
                        _type = TfsExtensions.ChangeTypes.FirstOrDefault(x => x.Value.IsEqual(changeType)).Key;
                    else
                        _type |= TfsExtensions.ChangeTypes.FirstOrDefault(x => x.Value.IsEqual(changeType)).Key;
                }
            }

            return _type ?? ChangeType.None;
        }
    }
}