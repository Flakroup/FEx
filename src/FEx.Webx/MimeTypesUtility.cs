using MimeMapping;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Webx;

public static class MimeTypesUtility
{
    public static ConcurrentDictionary<string, string> TypeMap { get; }
    public static IReadOnlyDictionary<string, string> Mappings => TypeMap;

    static MimeTypesUtility()
    {
        try
        {
            var typeMap =
                new ConcurrentDictionary<string, string>(MimeUtility.TypeMap.ToDictionary(x => x.Key, x => x.Value));

            typeMap.TryAdd(".*", "application/octet-stream");

            TypeMap = typeMap;
        }
        catch
        {
            //ignored
        }
    }
}