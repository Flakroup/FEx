using FEx.Extensions.Collections.Dictionaries;
using FEx.Extensions.IO;
using FEx.Utilities.IO;

namespace FEx.Utilities.Extensions;

public static class DirectoryInfoExtensions
{
    public static string GetSpecialDirectoryPathDescendants(this Environment.SpecialFolder folder, params string[] descendants)
    {
        SpecialDirectory dir = GetSpecialDirectory(folder);
        return dir.Directory.GetDescendantPath(descendants);
    }

    public static SpecialDirectory GetSpecialDirectory(this Environment.SpecialFolder folder)
    {
        return SpecialDirectory.SpecialDirectories.TryGetKeyValue(folder);
    }
}