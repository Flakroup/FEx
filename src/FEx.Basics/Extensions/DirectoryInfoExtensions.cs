using FEx.Basics.IO;
using FEx.Extensions.Collections.Dictionaries;
using FEx.Extensions.IO;
using System;

namespace FEx.Basics.Extensions;

public static class DirectoryInfoExtensions
{
    public static string GetSpecialDirectoryPathDescendants(this Environment.SpecialFolder folder,
                                                            params string[] descendants)
    {
        SpecialDirectory dir = GetSpecialDirectory(folder);

        return dir.Directory.GetDescendantPath(descendants);
    }

    public static SpecialDirectory GetSpecialDirectory(this Environment.SpecialFolder folder) =>
        SpecialDirectory.SpecialDirectories.TryGetKeyValue(folder);
}