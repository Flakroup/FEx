using FEx.AzureDevOpsx.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace FEx.AzureDevOpsx.Responses;

public class ShelvesetResponse : BaseTfsResponse
{
    public static JsonSerializerSettings Settings { get; } = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        MissingMemberHandling = MissingMemberHandling.Error,
        Converters =
        {
            new IsoDateTimeConverter
            {
                DateTimeStyles = DateTimeStyles.AssumeUniversal
            }
        }
    };

    [JsonProperty("value")]
    public ShelvesetContent[] Value { get; set; }
}