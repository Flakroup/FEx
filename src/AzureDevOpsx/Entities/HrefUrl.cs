using Newtonsoft.Json;
using System;

namespace FEx.AzureDevOpsx.Entities;

public class HrefUrl
{
    [JsonProperty("href")]
    public Uri Href { get; set; }
}