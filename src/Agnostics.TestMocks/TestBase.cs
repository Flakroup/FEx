using Newtonsoft.Json;
using System.Linq;

namespace FEx.Agnostics.TestMocks;

public abstract class TestBase
{
    private readonly JsonSerializerSettings _serializerSettings;

    protected TestBase()
    {
        _serializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Include,
            DateFormatString = "yyyy/MM/dd HH:mm:ss",
            Formatting = Formatting.Indented
        };
    }

    protected string FormatMessage(string format, params object[] args) =>
        string.Format(format, args.Select(e => JsonConvert.SerializeObject(e, _serializerSettings)).ToArray<object>());
}