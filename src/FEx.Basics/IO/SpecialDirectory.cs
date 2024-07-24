using FEx.Common.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnumExtensions = FEx.Extensions.EnumExtensions;

namespace FEx.Basics.IO;

public class SpecialDirectory
{
    public static IReadOnlyDictionary<Environment.SpecialFolder, SpecialDirectory> SpecialDirectories
    {
        get;
        private set;
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

    static SpecialDirectory()
    {
        EnsureSpecialDirectories();
    }

    public static IDictionary<Environment.SpecialFolder, SpecialDirectory> GetExistingDirectories() => SpecialDirectories.Where(x => x.Value is not null && x.Value.Directory.Exists)
            .OrderBy(x => x.Value.FullName)
            .ToDictionary(x => x.Key, x => x.Value);

    public override string ToString() => FullName;

    private static void EnsureSpecialDirectories()
    {
        var source = EnumExtensions.GetEnumValues<Environment.SpecialFolder>()
            .Select(x => (key: x, value: EvaluateSpecialDirectory(x)))
            .ToDictionary(x => x.key, x => x.value);

        SpecialDirectories = new ConcurrentDictionary<Environment.SpecialFolder, SpecialDirectory>(source);
    }

    private static SpecialDirectory EvaluateSpecialDirectory(Environment.SpecialFolder value)
    {
        try
        {
            string path = Environment.GetFolderPath(value);

            if (path.IsNotNullOrEmptyString())
                return new(value, path);
        }
        catch
        {
            //ignored
        }

        return null;
    }
}