using Newtonsoft.Json;

namespace FEx.AzureDevOpsx.Responses;

public class BaseTfsResponse
{
    [JsonProperty("count")]
    public long Count { get; set; }
}