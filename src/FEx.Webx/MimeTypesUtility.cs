using MimeMapping;
using System.Collections.ObjectModel;
using System.Linq;

namespace FEx.Webx;

public static class MimeTypesUtility
{
    public static ReadOnlyDictionary<string, string> Mappings { get; }

    static MimeTypesUtility()
    {
        try
        {
            var typeMap = MimeUtility.TypeMap.ToDictionary(x => x.Key, x => x.Value);

            if (!typeMap.ContainsKey(".*"))
                typeMap.Add(".*", "application/octet-stream");

            Mappings = new(typeMap);
        }
        catch
        {
            //ignored
        }
    }
}