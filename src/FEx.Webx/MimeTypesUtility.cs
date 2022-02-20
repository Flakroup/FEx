using MimeMapping;
using System.Collections.ObjectModel;

namespace FEx.Webx;

public static class MimeTypesUtility
{
    static MimeTypesUtility()
    {
        try
        {
            Dictionary<string, string> typeMap = MimeUtility.TypeMap.ToDictionary(x => x.Key, x => x.Value);

            if (!typeMap.ContainsKey(".*"))
            {
                typeMap.Add(".*", "application/octet-stream");
            }

            Mappings = new ReadOnlyDictionary<string, string>(typeMap);
        }
        catch
        {
            //ignored
        }
    }

    public static ReadOnlyDictionary<string, string> Mappings { get; }
}