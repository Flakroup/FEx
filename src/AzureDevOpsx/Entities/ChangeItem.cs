using Newtonsoft.Json;
using System;

namespace FEx.AzureDevOpsx.Entities;

public class ChangeItem
{
    [JsonProperty("version")]
    public long? Version { get; set; }

    [JsonProperty("hashValue")]
    public string? HashValue { get; set; }

    [JsonProperty("path")]
    public string? Path { get; set; }

    [JsonProperty("url")]
    public Uri? Url { get; set; }

    [JsonProperty("isFolder")]
    public bool? IsFolder { get; set; }
}