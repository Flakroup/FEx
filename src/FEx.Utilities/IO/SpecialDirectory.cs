using FEx.Extensions;
using FEx.Extensions.Collections.Dictionaries;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FEx.Utilities.IO;

public class SpecialDirectory
{
    public static IDictionary<Environment.SpecialFolder, SpecialDirectory> SpecialDirectories { get; private set; }

    static SpecialDirectory()
    {
        EnsureSpecialDirectories();
    }

    private static void EnsureSpecialDirectories()
    {
        SpecialDirectories ??= new ConcurrentDictionary<Environment.SpecialFolder, SpecialDirectory>();

        SpecialDirectories.SyncWith(EnumExtensions.GetEnumValues<Environment.SpecialFolder>()
            .Distinct()
            .Select(x => (key: x, value: EvaluateSpecialDirectory(x)))
            .ToDictionary(x => x.key, x => x.value));
    }

    private static SpecialDirectory EvaluateSpecialDirectory(Environment.SpecialFolder value)
    {
        try
        {
            string path = Environment.GetFolderPath(value);

            if (path.IsNotNullOrEmptyString())
                return new SpecialDirectory(value, path);
        }
        catch
        {
            //ignored
        }

        return null;
    }

    public Environment.SpecialFolder DirectoryType { get; }
    public DirectoryInfo Directory { get; }
    public string FullName => Directory?.FullName;

    public SpecialDirectory(Environment.SpecialFolder directoryType, DirectoryInfo directory)
    {
        DirectoryType = directoryType;
        Directory = directory;
    }

    public SpecialDirectory(Environment.SpecialFolder directoryType, string directory)
        : this(directoryType, new DirectoryInfo(directory))
    {
    }
}