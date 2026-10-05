using System.Collections.Generic;
using System.Text.Json.Serialization;
using NewtonsoftConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NJsonPathConverter = FEx.Json.Converters.JsonPathConverter;
using NParseStringToLongConverter = FEx.Json.Converters.ParseStringToLongConverter;
using SJsonPathConverter = FEx.Json.SystemTextJsonx.Converters.JsonPathConverter<FEx.Json.Tests.Contract.PathModel>;
using SParseStringToLongConverter = FEx.Json.SystemTextJsonx.Converters.ParseStringToLongConverter;

namespace FEx.Json.Tests.Contract;

// The models carry the attributes of both libraries, so one suite drives both implementations.

public sealed class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public List<string> Tags { get; set; } = [];
    public Address? Address { get; set; }
}

public sealed class Address
{
    public string City { get; set; } = string.Empty;
}

public sealed class LongHolder
{
    [NewtonsoftConverter(typeof(NParseStringToLongConverter))]
    [JsonConverter(typeof(SParseStringToLongConverter))]
    public long Amount { get; set; }
}

public sealed class DoubleHolder
{
    // Both implementations apply ParseStringToDoubleConverter to every double by default.
    public double Value { get; set; }

    public double? Optional { get; set; }
}

/// <summary>Two members claim the JSON name "x": an invalid contract in both libraries.</summary>
public sealed class CollidingModel
{
    [NewtonsoftProperty("x")]
    [JsonPropertyName("x")]
    public int First { get; set; }

    [NewtonsoftProperty("x")]
    [JsonPropertyName("x")]
    public int Second { get; set; }
}

[NewtonsoftConverter(typeof(NJsonPathConverter))]
[JsonConverter(typeof(SJsonPathConverter))]
public sealed class PathModel
{
    [NewtonsoftProperty("data.items[1].name")]
    [JsonPropertyName("data.items[1].name")]
    public string? Name { get; set; }

    [NewtonsoftProperty("data.count")]
    [JsonPropertyName("data.count")]
    public int Count { get; set; }

    public string? Flat { get; set; }

    // A member name starting with '$' that is not the JSONPath root.
    [NewtonsoftProperty("$schema")]
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }
}

public interface IDIModel
{
    string Name { get; set; }
}

public sealed class DIModel : IDIModel
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Not in any payload, so it tells who created the instance.</summary>
    public string Origin { get; set; } = "constructor";
}
