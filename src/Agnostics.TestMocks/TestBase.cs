using Newtonsoft.Json;
using System.Linq;

namespace FEx.Agnostics.TestMocks;

/// <summary>Base class for tests that provides helpers for building diagnostic messages.</summary>
public abstract class TestBase
{
    private readonly JsonSerializerSettings _serializerSettings;

    /// <summary>Initializes the base class with the JSON settings used to render message arguments.</summary>
    protected TestBase()
    {
        _serializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Include,
            DateFormatString = "yyyy/MM/dd HH:mm:ss",
            Formatting = Formatting.Indented
        };
    }

    /// <summary>Formats a message after serializing each argument to indented JSON.</summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">The arguments to serialize and insert into <paramref name="format"/>.</param>
    /// <returns>The formatted message.</returns>
    protected string FormatMessage(string format, params object[] args) =>
        string.Format(format, args.Select(e => JsonConvert.SerializeObject(e, _serializerSettings)).ToArray<object>());
}