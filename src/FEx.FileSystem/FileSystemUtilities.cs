using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Collections.Concurrent;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD
using FEx.Agnostics.Abstractions.Extensions.Interop;
#endif

namespace FEx.FileSystem;

public class FileSystemUtilities
{
    public static int PrgValue;
    public static int PrgMax;
    private static readonly SemaphoreSlim _progressSemaphore = new(1, 1);

    private static readonly bool _isReportingCapable = !Console.IsOutputRedirected;
    private static bool _isReporting;
    private static ILogger _logger;

    protected static ILogger Logger => _logger ??= FExServiceProvider.Get<ILogger>();

    public static Result<DirectoryInfo, ExceptionError> DirectoryPathStringToDirectoryInfo(string source)
    {
        try
        {
            if (source is null)
                return Result<DirectoryInfo, ExceptionError>.Failure;

            if (source.EndsWith("\\")
                || source.EndsWith("/"))
                source = source.TrimEnd('\\', '/');

            return new DirectoryInfo(source);
        }
        catch (Exception e)
        {
            return new ExceptionError(e);
        }
    }

    public static Result<AggregatedError> ProcessDirectory(string source,
                                                           string dest,
                                                           FileOperation fileOperation,
                                                           bool printPaths = true,
                                                           bool printLog = true,
                                                           params string[] exclusionPaths)
    {
        Result<DirectoryInfo, ExceptionError> sourceResult = DirectoryPathStringToDirectoryInfo(source);
        Result<DirectoryInfo, ExceptionError> destResult = DirectoryPathStringToDirectoryInfo(dest);

        var errors = sourceResult.Error.Yield().Concat(destResult.Error.Yield()).Where(x => x is not null).ToList();

        if (errors.Count > 0)
            return new AggregatedError(errors);

        return ProcessDirectory(sourceResult.Data,
            destResult.Data,
            fileOperation,
            printPaths,
            printLog,
            exclusionPaths);
    }

    public static Result<AggregatedError> ProcessDirectory(DirectoryInfo sourceInfo,
                                                           DirectoryInfo dest,
                                                           FileOperation fileOperation,
                                                           bool printPaths = true,
                                                           bool printLog = true,
                                                           params string[] exclusionPaths)
    {
        try
        {
            Log("Scanning source directories", printLog);

            Result<ConcurrentDictionary<DirectoryInfo, FileInfo[]>, AggregatedError> sourceDirectoriesResult =
                GetDirectoriesContents(sourceInfo, exclusionPaths);

            if (sourceDirectoriesResult.IsFailure)
                return sourceDirectoriesResult.Error;

            ConcurrentDictionary<DirectoryInfo, FileInfo[]> sourceDirectories = sourceDirectoriesResult.Data;

            if (sourceDirectories?.Count > 0)
            {
                string operationString = fileOperation switch
                {
                    FileOperation.Copy => "copied",
                    FileOperation.Move => "moved",
                    FileOperation.Delete => "deleted",
                    FileOperation.SyncSrcToDest => "synchronized",
                    _ => string.Empty
                };

                ConcurrentList<FileInfo> files = GetSourceFiles(sourceDirectories);

                Log(
                    $"There are {sourceDirectories.Keys.Count} directories with total {files.Count} files to be {operationString}",
                    printLog);

                Result<AggregatedError> result = Result<AggregatedError>.Success;

                if (fileOperation == FileOperation.SyncSrcToDest)
                {
                    Log("Removing files and directories not present in source from destination directory", printLog);

                    result = DeleteNotPresentElements(sourceDirectories,
                        dest,
                        sourceInfo,
                        printPaths,
                        printLog,
                        exclusionPaths);
                }

                if (fileOperation is FileOperation.Copy or FileOperation.Move or FileOperation.SyncSrcToDest)
                {
                    Log("Creating destination directories structure", printLog);
                    CreateMissingDestinationDirectories(dest, sourceInfo, sourceDirectories.Keys);
                }

                Log(
                    $"Proceeding with {fileOperation.GetEnumValueDescription().ToLower()} operation {sourceInfo.FullName}",
                    printLog);

                switch (fileOperation)
                {
                    case FileOperation.Copy or FileOperation.Move or FileOperation.SyncSrcToDest:
                        {
                            var results = new ConcurrentDictionary<DirectoryInfo, Result<ExceptionError>>();
                            PrgMax = files.Count;

                            Parallel.ForEach(files,
                                file => ProcessFile(dest, file, sourceInfo, results, fileOperation, printPaths));

                            var errors = results.Values.Where(x => x.IsFailure).Select(x => x.Error).ToList();

                            result = errors.Count > 0
                                ? new AggregatedError(result.Error.InnerErrors.Concat(errors).ToList().AsReadOnly())
                                : Result<AggregatedError>.Success;

                            break;
                        }
                    case FileOperation.Delete:
                        {
                            var results = new ConcurrentDictionary<FileInfo, Result<ExceptionError>>();

                            Parallel.ForEach(files,
                                file =>
                                {
                                    Result<ExceptionError> temp;

                                    try
                                    {
                                        temp = SafeDeleteFile(file);
                                    }
                                    catch (Exception ex)
                                    {
                                        temp = new ExceptionError(ex);
                                    }

                                    results.AddOrUpdateValue(file, temp);
                                });

                            var errors = results.Values.Where(x => x.IsFailure).Select(x => x.Error).ToList();

                            result = errors.Count > 0
                                ? new AggregatedError(errors)
                                : Result<AggregatedError>.Success;

                            break;
                        }
                }

                FinishProgress();

                if (result.IsSuccess
                    && fileOperation is FileOperation.Move or FileOperation.Delete)
                {
                    Log("Removing empty source directory", printLog);

                    Result<ConcurrentDictionary<DirectoryInfo, FileInfo[]>, AggregatedError> sourceDirsResult =
                        GetDirectoriesContents(sourceInfo, exclusionPaths);

                    if (sourceDirsResult.IsSuccess)
                    {
                        sourceDirectories = sourceDirsResult.Data;
                        files = GetSourceFiles(sourceDirectories);

                        if (!files.Any())
                            sourceInfo.Delete(true);
                        else
                            return new Error($"ERROR: There are files left in source directory: {sourceInfo.FullName}")
                                .ToAggregatedError();
                    }
                }

                return Result<AggregatedError>.Success;
            }

            return new Error($"ERROR: Path not found: {sourceInfo.FullName}").ToAggregatedError();
        }
        catch (Exception e)
        {
            Log(e.Message, printLog, LogLevel.Error, e);

            return new ExceptionError(e).ToAggregatedError();
        }
    }

    public static ConcurrentList<FileInfo> GetSourceFiles(
        ConcurrentDictionary<DirectoryInfo, FileInfo[]> sourceDirectories)
    {
        if (sourceDirectories?.Count > 0)
        {
            var result = new ConcurrentList<FileInfo>();
            Parallel.ForEach(sourceDirectories, sourceDirectory => result.AddRange(sourceDirectory.Value));

            return result;
        }

        return [];
    }

    public static Result<Error> CreateMissingDestinationDirectories(FileSystemInfo dest,
                                                                    ICollection<string> sourcePaths)
    {
        try
        {
            if (sourcePaths?.Count > 0)
            {
                var sourceDirectories = new Dictionary<string, List<DirectoryInfo>>();

                var dirs = sourcePaths.Select(path => (Path: path, Result: IsPathFile(path)))
                    .Select(tuple => tuple.Result.IsSuccess && tuple.Result.Data
                        ? new FileInfo(tuple.Path).Directory
                        : new(tuple.Path))
                    .DistinctBy(x => x.FullName)
                    .ToList();

                for (var i = 0; i < dirs.Count; i++)
                {
                    DirectoryInfo dir = dirs[i];
                    dirs.RemoveAt(i);
                    i--;

                    if (!dirs.Any(x => x.FullName.Contains(dir.FullName)))
                    {
                        if (!sourceDirectories.ContainsKey(dir.Root.FullName))
                            sourceDirectories.Add(dir.Root.FullName,
                            [
                                dir
                            ]);
                        else
                            sourceDirectories[dir.Root.FullName].Add(dir);
                    }
                }

                foreach (string root in sourceDirectories.Keys)
                {
                    DirectoryInfo sourceInfo = sourceDirectories[root]
                        .OrderByDescending(x => x.FullName.Length)
                        .First();

                    while (!sourceDirectories[root]
                               .All(x => x.FullName.Contains(sourceInfo?.FullName
                                                             ?? throw new InvalidOperationException())))
                        sourceInfo = sourceInfo?.Parent;

                    CreateMissingDestinationDirectories(dest, sourceInfo, sourceDirectories[root]);
                }
            }
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }

        return Result<Error>.Success;
    }

    public static Result<ConcurrentDictionary<DirectoryInfo, FileInfo[]>, AggregatedError> GetDirectoriesContents(
        string source,
        string[] exclusionPaths)
    {
        Result<DirectoryInfo, ExceptionError> sourceResult = DirectoryPathStringToDirectoryInfo(source);

        if (sourceResult.IsFailure)
            return sourceResult.Error.ToAggregatedError();

        return GetDirectoriesContents(sourceResult.Data, exclusionPaths);
    }

    public static Result<ConcurrentDictionary<DirectoryInfo, FileInfo[]>, AggregatedError> GetDirectoriesContents(
        DirectoryInfo source,
        params string[] exclusionPaths) =>
        FlattenDirectoriesTree(source, [.. exclusionPaths]);

    public static Result<ExceptionError> CopyFileAndSetAttributes(FileSystemInfo destDir,
                                                                  FileInfo sourceFile,
                                                                  bool printPaths,
                                                                  bool copyOnlyIfDifferent = false,
                                                                  bool printLog = true)
    {
        try
        {
            if (sourceFile.Exists)
            {
                string destFile = Path.Combine(destDir.FullName, Path.GetFileName(sourceFile.FullName));
                var destFileInfo = new FileInfo(destFile);
                var toCopy = true;

                if (destFileInfo.Exists)
                {
                    destFileInfo.Attributes &=
                        ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);

                    if (copyOnlyIfDifferent)
                        toCopy = destFileInfo.Length != sourceFile.Length
                                 || destFileInfo.LastWriteTime != sourceFile.LastWriteTime;
                }

                if (toCopy)
                {
                    using (var src = new FileStream(sourceFile.FullName,
                               FileMode.Open,
                               FileAccess.Read,
                               FileShare.Read))
                    {
                        using var dst = new FileStream(destFileInfo.FullName,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite,
                            FileShare.ReadWrite);

                        dst.SetLength(0);
                        dst.Seek(0, SeekOrigin.Begin);
                        src.CopyTo(dst);
                    }

                    destFileInfo.Refresh();
                    destFileInfo.CreationTime = sourceFile.CreationTime;
                    destFileInfo.LastAccessTime = sourceFile.LastAccessTime;
                    destFileInfo.LastWriteTime = sourceFile.LastWriteTime;

                    //Clear readonly
                    if (destFileInfo.IsReadOnly)
                        destFileInfo.IsReadOnly = false;

                    if (printPaths)
                        Log($"Copied file : {destFile}", printLog);
                }

                if (!printPaths)
                {
                    Interlocked.Increment(ref PrgValue);

                    FExCoreStatics.AsyncHelper.FireTaskAndForget(() => ReportProgressAsync(destFile, GetPercentage),
                        AsyncMode.ThreadPool);
                }
            }
            else
            {
                throw new FileNotFoundException("Source file doesn't exist.", sourceFile.FullName);
            }
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }

        return Result<ExceptionError>.Success;
    }

    public static Result<FileInfo, ExceptionError> MoveFileAndSetAttributes(FileSystemInfo destDir,
                                                                            FileInfo sourceFile,
                                                                            bool printPaths,
                                                                            bool printLog = true)
    {
        try
        {
            string destFile = Path.Combine(destDir.FullName, Path.GetFileName(sourceFile.FullName));
            FileInfo destFileInfo = new(destFile);
            Result<ExceptionError> deleteResult = SafeDeleteFile(destFileInfo);

            if (deleteResult.IsSuccess)
            {
                while (sourceFile.Exists
                       && !destFileInfo.Exists)
                {
                    while (destFileInfo.Directory?.Exists == false)
                    {
                        destFileInfo.Directory.Create();
                        destFileInfo.Refresh();
                    }

                    sourceFile.MoveTo(destFileInfo.FullName);
                    sourceFile.Refresh();
                    destFileInfo.Refresh();
                }

                if (printPaths)
                    Log($"Moved file : {destFile}", printLog);

                if (DirectoryIsEmpty(sourceFile.Directory?.FullName))
                    sourceFile.Directory?.Delete(true);

                destFileInfo.Refresh();

                //Clear readonly
                if (destFileInfo.IsReadOnly)
                    destFileInfo.IsReadOnly = false;
            }

            return destFileInfo;
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    public static Result<ExceptionError> SafeDeleteFile(string filePath, bool removeEmptyDirectory = false)
    {
        if (filePath is not null)
        {
            var fileInfo = new FileInfo(filePath);

            return SafeDeleteFile(fileInfo, removeEmptyDirectory);
        }

        return Result<ExceptionError>.Failure;
    }

    public static Result<ExceptionError> SafeDeleteFile(FileInfo fileInfo, bool removeEmptyDirectory = false)
    {
        try
        {
            fileInfo.Refresh();

            // remove archive and readonly flags if it exists
            if (fileInfo.Exists)
            {
                fileInfo.Attributes &= ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
                //File.SetAttributes(fileInfo.FullName, FileAttributes.Normal);
                fileInfo.Delete();

                if (removeEmptyDirectory && DirectoryIsEmpty(fileInfo.Directory?.FullName))
                    fileInfo.Directory?.Delete(true);
            }

            return Result<ExceptionError>.Success;
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    public static bool DirectoryIsEmpty(string directoryPath) =>
        !Directory.EnumerateFileSystemEntries(directoryPath).Any();

    /// <summary>
    /// Fixes the name of the file.
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    /// <returns></returns>
    public static string FixFileName(string fileName) =>
        Path.GetInvalidFileNameChars().Aggregate(fileName, (current, ch) => current.Replace(ch.ToString(), "_"));

    public static Result<bool, ExceptionError> IsPathFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            FileAttributes attr = File.GetAttributes(path);

            return !attr.HasFlag(FileAttributes.Directory);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    /// <summary>
    /// Gets file encoding with or without Byte Order Mark
    /// </summary>
    /// <param name="path">The path to file.</param>
    /// <param name="omitBom">if set to <c>true</c> omits BOM.</param>
    /// <returns>Encoding.</returns>
    public static Encoding GetEncoding(string path, bool omitBom = false)
    {
        Encoding enc = null;

        try
        {
            var sr = new StreamReader(path, true);

            while (sr.Peek() >= 0)
                sr.Read();

            enc = Encoding.GetEncoding(Convert.ToInt32(sr.CurrentEncoding.CodePage.ToString()));
            sr.Close();
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }

        return omitBom
            ? OmitBom(enc)
            : enc;
    }

    /// <summary>
    /// Omits the bom.
    /// </summary>
    /// <param name="enc">The encoding.</param>
    /// <returns><see cref="Encoding" />.</returns>
    public static Encoding OmitBom(Encoding enc) =>
        Equals(enc, Encoding.GetEncoding(Convert.ToInt32(new UTF8Encoding().CodePage.ToString())))
            ? new UTF8Encoding(false)
            : enc;

    public static async Task<Result<Error>> ReplaceStringInFileAsync(FileInfo fileInfo,
                                                                     string oldValue,
                                                                     string newValue)
    {
        try
        {
            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = false;
                string data;

                using (StreamReader sr = fileInfo.OpenText())
                    data = await sr.ReadToEndAsync();

                var s = new StringBuilder(data, data.Length * 2);
                s.Replace(oldValue, newValue);
#if !NETSTANDARD2_0
                await
#endif
                    using FileStream fs = fileInfo.OpenWrite();

                fs.SetLength(0);
#if !NETSTANDARD2_0
                await
#endif
                    using var sw = new StreamWriter(fs);

                await sw.WriteAsync(s.ToString());
                await sw.FlushAsync();

                return Result<Error>.Success;
            }

            return new Error($"File {fileInfo.FullName} doesn't exist.");
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    public static bool FileExistsSafe(string path) => IsPathFile(path).TryGetData(out bool isFile) && isFile;

    public static Result<AggregatedError> SafeDeleteDirectory(string source,
                                                              bool printPaths = true,
                                                              bool printLog = true,
                                                              params string[] exclusionPaths)
    {
        Result<DirectoryInfo, ExceptionError> result = DirectoryPathStringToDirectoryInfo(source);

        if (result.IsFailure)
            return result.Error.ToAggregatedError();

        return ProcessDirectory(result.Data, null, FileOperation.Delete, printPaths, printLog, exclusionPaths);
    }

    private static void FinishProgress(bool printLog = true)
    {
        if (_isReportingCapable)
        {
            _progressSemaphore.Wait();
            _isReporting = true;
            Log("\r" + new string(' ', Console.WindowWidth - 1) + "\r", printLog);

            Console.SetCursorPosition(0,
                Console.CursorTop > 0
                    ? Console.CursorTop - 1
                    : Console.CursorTop);

            PrgValue = 0;
            PrgMax = 0;
            _isReporting = false;
            _progressSemaphore.Release();
        }
    }

    private static Result<AggregatedError> DeleteNotPresentElements(
        ConcurrentDictionary<DirectoryInfo, FileInfo[]> sourceDirectories,
        DirectoryInfo dest,
        DirectoryInfo src,
        bool printPaths = true,
        bool printLog = true,
        params string[] exclusionPaths)
    {
        Result<ConcurrentDictionary<DirectoryInfo, FileInfo[]>, AggregatedError> destDirectoriesResult =
            GetDirectoriesContents(dest, exclusionPaths);

        if (destDirectoriesResult.IsFailure)
            return destDirectoriesResult.Error;

        ConcurrentDictionary<DirectoryInfo, FileInfo[]> destDirectories = destDirectoriesResult.Data;

        try
        {
            if (!destDirectories.IsEmpty)
            {
                var errors = new List<Error>();

                foreach (KeyValuePair<DirectoryInfo, FileInfo[]> dirContent in destDirectories)
                {
                    string relPath = dirContent.Key.FullName.Substring(dest.FullName.Length);
                    string relatedSourceDir = PathCombine(src.FullName, relPath);

                    DirectoryInfo currSrcDir =
                        sourceDirectories.Keys.FindInEnumerable(x => x.FullName == relatedSourceDir);

                    if (currSrcDir is null)
                    {
                        Result<AggregatedError> directoryResult = ProcessDirectory(dirContent.Key,
                            null,
                            FileOperation.Delete,
                            printPaths,
                            printLog,
                            exclusionPaths);

                        if (directoryResult.IsFailure)
                            errors.Add(directoryResult.Error);
                    }
                    else
                    {
                        errors.AddRange(dirContent.Value
                            .Where(fileInfo => sourceDirectories[currSrcDir].All(x => x.Name != fileInfo.Name))
                            .Select(fileInfo => SafeDeleteFile(fileInfo))
                            .Where(fileDeleteResult => fileDeleteResult.IsFailure)
                            .Select(fileDeleteResult => fileDeleteResult.Error));
                    }
                }

                if (errors.Count > 0)
                    return new AggregatedError(errors.AsReadOnly());
            }

            return Result<AggregatedError>.Success;
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex).ToAggregatedError();
        }
    }

    private static string PathCombine(string path1, string path2)
    {
        while (path2.StartsWith("/")
               || path2.StartsWith("\\"))
            path2 = path2.TrimStart('/', '\\');

        return Path.Combine(path1, path2);
    }

    private static void ProcessFile(DirectoryInfo dest,
                                    FileInfo file,
                                    FileSystemInfo sourceInfo,
                                    ConcurrentDictionary<DirectoryInfo, Result<ExceptionError>> results,
                                    FileOperation fileOperation,
                                    bool printPaths)
    {
        if (file is not null)
        {
            string relativePath = file.DirectoryName?.Replace(sourceInfo.FullName, string.Empty).TrimStart('\\', '/');
            var destDir = new DirectoryInfo(Path.Combine(dest.FullName, relativePath ?? string.Empty));
            var temp = new Result<ExceptionError>();

            try
            {
                if (destDir.Exists)
                    temp = fileOperation switch
                    {
                        FileOperation.Copy => CopyFileAndSetAttributes(destDir, file, printPaths),
                        FileOperation.Move => MoveFileAndSetAttributes(destDir, file, printPaths).Error,
                        FileOperation.SyncSrcToDest => CopyFileAndSetAttributes(destDir, file, printPaths, true),
                        _ => temp
                    };
            }
            catch (Exception e)
            {
                temp = new ExceptionError(e);
            }

            results.AddOrUpdateValue(destDir, temp);
        }
    }

    private static void CreateMissingDestinationDirectories(FileSystemInfo dest,
                                                            FileSystemInfo sourceInfo,
                                                            ICollection<DirectoryInfo> sourceDirectories)
    {
        if (sourceDirectories?.Count > 0)
        {
            DirectoryInfo[] destDirectories =
            [
                .. sourceDirectories.Select(dir => new DirectoryInfo(Path.Combine(dest.GetDirectory().FullName,
                        dir.FullName.Replace(sourceInfo.FullName, string.Empty).TrimStart('\\', '/'))))
                    .DistinctBy(x => x.FullName)
            ];

            foreach (DirectoryInfo dir in destDirectories.Where(dir => !dir.Exists))
                dir.Create();
        }
    }

    private static Result<ConcurrentDictionary<DirectoryInfo, FileInfo[]>, AggregatedError> FlattenDirectoriesTree(
        DirectoryInfo root,
        ICollection<string> exclusionPaths)
    {
        var res = new ConcurrentDictionary<DirectoryInfo, FileInfo[]>();

        try
        {
            root.Refresh();

            if (root.Exists
                && !exclusionPaths.Contains(root.FullName))
            {
                Result<AggregatedError> result = FlattenDirectoriesTree(root, res, exclusionPaths);
                FinishProgress();

                if (result.IsFailure)
                    return result.Error;
            }
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex).ToAggregatedError();
        }

        return res;
    }

    private static Result<AggregatedError> FlattenDirectoriesTree(DirectoryInfo root,
                                                                  ConcurrentDictionary<DirectoryInfo, FileInfo[]>
                                                                      flatTree,
                                                                  ICollection<string> exclusionPaths)
    {
        try
        {
            flatTree.AddOrUpdate(root,
                _ => root.GetFiles(),
                (_, _) =>
                [
                    .. root.GetFiles()
                        .Where(file => !exclusionPaths.Any(exclusion =>
                            exclusion.Equals(file.FullName, StringComparison.OrdinalIgnoreCase)))
                ]);

            DirectoryInfo[] subDirs = [.. root.GetDirectories().Where(x => !exclusionPaths.Contains(x.FullName))];
            Interlocked.Add(ref PrgValue, subDirs.Length);

            FExCoreStatics.AsyncHelper.FireTaskAndForget(
                () => ReportProgressAsync(root.FullName, () => PrgValue.ToString()),
                AsyncMode.ThreadPool);

            if (subDirs.Length > 0)
            {
                var results = new ConcurrentDictionary<DirectoryInfo, Result<ExceptionError>>();

                Parallel.ForEach(subDirs,
                    dir =>
                    {
                        var temp = new Result<ExceptionError>();

                        try
                        {
                            FlattenDirectoriesTree(dir, flatTree, exclusionPaths);
                        }
                        catch (Exception e)
                        {
                            temp = new ExceptionError(e);
                        }

                        results.AddOrUpdateValue(dir, temp);
                    });

                var fails = results.Values.Where(x => x.IsFailure).Select(x => x.Error).ToList();

                if (fails.Count > 0)
                    return new AggregatedError(fails);
            }

            return Result<AggregatedError>.Success;
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex).ToAggregatedError();
        }
    }

    private static string GetPercentage()
    {
        int prgVal = PrgValue;
        int prgMax = PrgMax;
        double perc = Math.Round((double)prgVal / prgMax * 100, 0);

        return $"{perc}% {prgVal}/{prgMax}";
    }

    private static async Task ReportProgressAsync(string msg, Func<string> msgPrefix = null, bool printLog = true)
    {
        if (_isReportingCapable && !_isReporting)
        {
            await _progressSemaphore.WaitAsync();
            _isReporting = true;

            Log("\r"
                + new string(' ', Console.WindowWidth - 1)
                + "\r"
                + (msgPrefix is not null
                    ? $"{msgPrefix()} {msg}"
                    : msg),
                printLog);

            Console.SetCursorPosition(0,
                Console.CursorTop > 0
                    ? Console.CursorTop - 1
                    : Console.CursorTop);

            _isReporting = false;
            _progressSemaphore.Release();
        }
    }

    private static void Log(string message,
                            bool printLog,
                            LogLevel level = LogLevel.Information,
                            Exception exception = null)
    {
        if (printLog && (message.IsNotNullOrEmptyString() || exception is not null))
            Logger.Log(level, exception, message);
    }
}