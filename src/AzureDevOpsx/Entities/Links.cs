using Newtonsoft.Json;

namespace FEx.AzureDevOpsx.Entities;

public class Links
{
    [JsonProperty("self")]
    public HrefUrl Self { get; set; }

    [JsonProperty("changes")]
    public HrefUrl Changes { get; set; }

    [JsonProperty("workItems")]
    public HrefUrl WorkItems { get; set; }

    [JsonProperty("owner")]
    public HrefUrl Owner { get; set; }
}
