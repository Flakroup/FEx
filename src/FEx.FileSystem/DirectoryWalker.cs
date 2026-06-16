using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Helpers;
using FEx.Core.Abstractions;
using FEx.Core.Collections.Concurrent;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.FileSystem;

public static class DirectoryWalker
{
    public static uint Limit
    {
        get => Queue.ConcurrencyLimit;
        set => Queue.ConcurrencyLimit = value;
    }

    private static ConcurrentHashSet<string> ErrorPaths { get; }

    private static FExEnumerationOptions DefaultOptions => FExEnumerationOptions.Compatible;
    private static AsyncProcessingQueue Queue { get; }

    static DirectoryWalker()
    {
        ErrorPaths = [];
        Queue = new();
    }

    /// <summary>
    /// Recursively gets all subdirectories from a root directory, ignoring
    /// any directories that throw an <see cref="UnauthorizedAccessException" /> or other IO exceptions.
    /// </summary>
    public static async Task<List<DirectoryInfo>> SafeGetAllDirectoriesAsync(string rootPath,
                                                                             DirectoryFilterDelegate predicate = null,
                                                                             string searchPattern = "*",
                                                                             FExEnumerationOptions options = null,
                                                                             DirectoryFilterDelegate
                                                                                 skipRecursionPredicate = null) =>
        await new DirectoryInfo(rootPath).SafeGetAllDirectoriesAsync(predicate,
            searchPattern,
            options,
            skipRecursionPredicate);

    /// <summary>
    /// Recursively gets all subdirectories from a root directory, ignoring
    /// any directories that throw an UnauthorizedAccessException or other IO exceptions.
    /// </summary>
    public static async Task<List<DirectoryInfo>> SafeGetAllDirectoriesAsync(this DirectoryInfo root,
                                                                             DirectoryFilterDelegate predicate = null,
                                                                             string searchPattern = "*",
                                                                             FExEnumerationOptions options = null,
                                                                             DirectoryFilterDelegate
                                                                                 skipRecursionPredicate = null)
    {
        if (!root.Exists)
            throw new DirectoryNotFoundException($"Specified path doesn't exist: {root.FullName}");

        var hasFilter = predicate is not null;
        options ??= DefaultOptions;

        return await GetDirectoriesAsync(root, hasFilter, predicate, searchPattern, options, skipRecursionPredicate);
    }

    public static List<FileInfo> SafeGetAllFiles(this DirectoryInfo root,
                                                 FileFilterDelegate predicate = null,
                                                 string searchPattern = "*",
                                                 FExEnumerationOptions options = null,
                                                 DirectoryFilterDelegate skipDirectoryPredicate = null)
    {
        if (!root.Exists)
            throw new DirectoryNotFoundException($"Specified path doesn't exist: {root.FullName}");

        var hasFilter = predicate is not null;
        var hasSkip = skipDirectoryPredicate is not null;
        options ??= DefaultOptions;

        var stack = new Stack<DirectoryInfo>();
        stack.Push(root);

        var result = new List<FileInfo>();

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current.IsErrorPath())
                continue;

            try
            {
                result.AddRange(current.EnumerateFiles(searchPattern, options)
                    .Where(file => !hasFilter || predicate!(file)));

                foreach (var subDir in current.EnumerateDirectories("*", options ?? DefaultOptions))
                {
                    if (!hasSkip
                        || !skipDirectoryPredicate!(subDir))
                        stack.Push(subDir);
                }
            }
            catch
            {
                current.AddToErrorPaths();
            }
        }

        return result;
    }

    public static void AddToErrorPaths(this DirectoryInfo folder) =>
        ErrorPaths.Add(folder.FullName + Path.DirectorySeparatorChar);

    public static bool IsErrorPath(this DirectoryInfo folder)
    {
        var fullPath = folder.FullName + Path.DirectorySeparatorChar;

        return ErrorPaths.Contains(fullPath) || ErrorPaths.Any(folder.FullName.StartsWith);
    }

    public static void ClearErrorPaths() => ErrorPaths.Clear();

    public static async Task<List<DirectoryInfo>> SafeGetLeafDirectoriesAsync(this DirectoryInfo root,
                                                                              DirectoryFilterDelegate predicate = null,
                                                                              string searchPattern = "*",
                                                                              FExEnumerationOptions options = null,
                                                                              DirectoryFilterDelegate
                                                                                  skipRecursionPredicate = null) =>
        await root.SafeGetAllDirectoriesAsync(dir => dir.IsLeaf() && (predicate is null || predicate(dir)),
            searchPattern,
            options,
            skipRecursionPredicate);

    /// <summary>
    /// Given a list of folders, returns only those which are not subfolders
    /// of another folder in the list ("top-level" relative to each other).
    /// </summary>
    public static List<DirectoryInfo> GetTopLevelFolders(this IList<DirectoryInfo> folders, bool logFindings = false)
    {
        List<DirectoryInfo> topLevelFolders = [];

        foreach (var folder in folders.OrderBy(static dir => dir.FullName, FExCoreStatics.AlphanumComparatorFast))
        {
            if (!folders.Any(parent =>
                    parent != folder && folder.FullName.StartsWith(parent.FullName + Path.DirectorySeparatorChar)))
            {
                topLevelFolders.Add(folder);

                if (logFindings)
                    Log.Debug($"Found {folder.FullName}");
            }
        }

        return topLevelFolders;
    }

    public static bool IsEmpty(this DirectoryInfo directory,
                               Func<IReadOnlyCollection<FileSystemInfo>, bool> predicate = null) =>
        RunSecure(directory,
            () =>
            {
                var contentsEnumerable = directory.EnumerateFileSystemInfos("*", DefaultOptions);

                if (predicate is null)
                    return !contentsEnumerable.Any();

                var contents = contentsEnumerable.ToArray();

                return contents.Length == 0 || predicate(contents);
            });

    public static bool IsLeaf(this DirectoryInfo directory) =>
        RunSecure(directory, () => !directory.EnumerateDirectories("*", DefaultOptions).Any());

    public static bool SafeDelete(this DirectoryInfo folder, bool recursive = false, bool logDeletions = false)
    {
        try
        {
            folder.Refresh();

            if (!folder.Exists)
                return true;

            if (recursive)
                return DeleteRecursive(folder, logDeletions);

            folder.Delete(false);

            if (logDeletions)
                Log.Debug("Deleted: {Path}", folder.FullName);

            return true;
        }
        catch (Exception ex)
        {
            Log.Warning("Cannot delete {Path}: {Message}", folder.FullName, ex.Message);

            return false;
        }
    }

    public static bool SafeDelete(this FileInfo file, bool logDeletions = false)
    {
        try
        {
            file.Refresh();

            if (file.Exists)
            {
                file.Delete();

                if (logDeletions)
                    Log.Debug("Deleted: {Path}", file.FullName);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Warning("Cannot delete {Path}: {Message}", file.FullName, ex.Message);

            return false;
        }
    }

    private static bool DeleteRecursive(DirectoryInfo folder, bool logDeletions)
    {
        try
        {
            folder.Delete(true);

            if (logDeletions)
                Log.Debug("Deleted: {Path}", folder.FullName);

            return true;
        }
        catch
        {
            // Fast path failed - fall back to manual enumeration (handles locked files)
        }

        var success = true;

        try
        {
            foreach (var file in folder.EnumerateFiles("*", DefaultOptions))
            {
                if (!file.SafeDelete(logDeletions))
                    success = false;
            }

            foreach (var subDir in folder.EnumerateDirectories("*", DefaultOptions))
            {
                if (!DeleteRecursive(subDir, logDeletions))
                    success = false;
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Cannot enumerate {Path}: {Message}", folder.FullName, ex.Message);

            return false;
        }

        if (!success)
            return false;

        try
        {
            folder.Delete(false);

            if (logDeletions)
                Log.Debug("Deleted: {Path}", folder.FullName);

            return true;
        }
        catch (Exception ex)
        {
            Log.Warning("Cannot delete {Path}: {Message}", folder.FullName, ex.Message);

            return false;
        }
    }

    private static async Task<List<DirectoryInfo>> GetDirectoriesAsync(DirectoryInfo current,
                                                                       bool hasFilter,
                                                                       DirectoryFilterDelegate predicate,
                                                                       string searchPattern,
                                                                       FExEnumerationOptions options,
                                                                       DirectoryFilterDelegate skipRecursionPredicate)
    {
        if (current.IsErrorPath())
            return [];

        try
        {
            var directories =
                await Queue.EnqueueAsync(() => Task.Run(() => current.GetDirectories(searchPattern, options)));

            var directoriesToRecurse = skipRecursionPredicate is not null
                ? directories.Where(dir => !skipRecursionPredicate(dir))
                : directories;

            var results = await directoriesToRecurse.WithWhenAllTasksAsync(dir =>
                GetDirectoriesAsync(dir, hasFilter, predicate, searchPattern, options, skipRecursionPredicate));

            return results.SelectMany(x => x)
                .Concat(directories.Where(subDir => !hasFilter || predicate(subDir)))
                .ToList();
        }
        catch
        {
            current.AddToErrorPaths();

            return [];
        }
    }

    private static T RunSecure<T>(DirectoryInfo directory, Func<T> func, T fallback = default)
    {
        if (directory.IsErrorPath())
            return fallback;

        try
        {
            return func();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"{ex.GetType().Name} for: {directory.FullName}");
            directory.AddToErrorPaths();

            return fallback;
        }
    }
}