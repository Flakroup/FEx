using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FEx.Agnostics.Abstractions.IO;

/// <summary>A well-known system folder together with its directory.</summary>
public class SpecialDirectory
{
    // Populated by the static constructor before any access.
    /// <summary>Gets the special folders that exist on the current system, keyed by folder type.</summary>
    public static IReadOnlyDictionary<Environment.SpecialFolder, SpecialDirectory> SpecialDirectories
    {
        get;
        private set;
    } = null!;

    /// <summary>Gets the special folder type.</summary>
    public Environment.SpecialFolder DirectoryType { get; }
    /// <summary>Gets the directory of the special folder.</summary>
    public DirectoryInfo Directory { get; }
    /// <summary>Gets the full path of the directory.</summary>
    public string FullName => Directory.FullName;

    /// <summary>Initializes the instance</summary>
    /// <param name="directoryType">The special folder type.</param>
    /// <param name="directory">The directory of the folder.</param>
    public SpecialDirectory(Environment.SpecialFolder directoryType, DirectoryInfo directory)
    {
        DirectoryType = directoryType;
        Directory = directory;
    }

    /// <summary>Initializes the instance from a path</summary>
    /// <param name="directoryType">The special folder type.</param>
    /// <param name="directory">The path of the folder.</param>
    public SpecialDirectory(Environment.SpecialFolder directoryType, string directory)
        : this(directoryType, new DirectoryInfo(directory))
    {
    }

    static SpecialDirectory()
    {
        EnsureSpecialDirectories();
    }

    /// <summary>Gets the special directories that exist on disk, ordered by path</summary>
    /// <returns>The existing special directories keyed by folder type.</returns>
    public static IDictionary<Environment.SpecialFolder, SpecialDirectory> GetExistingDirectories() =>
        SpecialDirectories.Where(x => x.Value is not null && x.Value.Directory.Exists)
            .OrderBy(x => x.Value.FullName)
            .ToDictionary(x => x.Key, x => x.Value);

    /// <summary>Returns the full path of the directory</summary>
    /// <returns>The full path.</returns>
    public override string ToString() => FullName;

    private static void EnsureSpecialDirectories()
    {
        var source = EnumExtensions.GetEnumValues<Environment.SpecialFolder>()
            .Select(x => (key: x, value: EvaluateSpecialDirectory(x)))
            .Where(x => x.value is not null)
            .ToDictionary(x => x.key, x => x.value!);

        SpecialDirectories = new ConcurrentDictionary<Environment.SpecialFolder, SpecialDirectory>(source);
    }

    private static SpecialDirectory? EvaluateSpecialDirectory(Environment.SpecialFolder value)
    {
        try
        {
            var path = Environment.GetFolderPath(value);

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