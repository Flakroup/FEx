using DynamicData;
using EFCore.BulkExtensions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Flow;
using FEx.EFCore.Collections;
using FEx.EFCore.Interfaces;
using FEx.FileSystem;
using FEx.Imaging.Windows.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FEx.Imaging.Windows;

public class IndexEntriesCache : SynchronizedDictionary<string, IndexEntry, FilesCacheContext>
{
    private const string FileName = "fileName";
    public IIndexEntryConfig Config { get; }

    public IndexEntriesCache(IEFCoreDatabaseBackedService<FilesCacheContext> dbService, IIndexEntryConfig config)
        : base(dbService, nameof(IndexEntry.AbsoluteUri))
    {
        Config = config;
        BeginInitialization();
    }

    // Base returns null only for addNew:false + missing key; callers passing addNew:false null-check the result (preserves pre-nullable behavior)
    public async Task<IndexEntry> GetOrAddValueAsync(string key, bool addNew = true, string? fileName = null) =>
        (await GetOrAddValueAsync(key,
            addNew,
            fileName is not null
                ? new Dictionary<string, object>
                {
                    [FileName] = fileName
                }
                : null))!;

    public async Task<int> RemoveIndexEntriesOfMissingFilesAsync(HashSet<string> existingFiles)
    {
        var removedEntries =
            await _dbSrv.RunTaskInDbContextAsync(ctx => RemoveIndexEntriesOfMissingFilesAsync(ctx, existingFiles));

        UseIndex = true;

        if (existingFiles.Count > 0)
        {
            var results = await existingFiles.WithWhenAllAsync(SafeDeleteFile);
            var errors = results.Where(x => x.IsFailure).Select(x => x.Error!).ToList();

            if (errors.Count > 0)
                throw new AggregateException(errors.Select(x => x.Exception).OfType<Exception>());
        }

        return removedEntries;
    }

    public async Task RemoveIndexEntriesAsync(IDictionary<string, FileInfo> entries)
    {
        if (entries.Count == 0)
            return;

        var results = (await entries.Values.WithWhenAllAsync(SafeDeleteFile)).Where(x => x.IsSuccess).ToList();

        if (results.Count > 0)
        {
            var paths = results.Select(x => x.Data.FullName).ToArray();
            entries.RemoveFromDictionaryWhere((_, v) => paths.Contains(v.FullName));
        }

        Cache.RemoveKeys(entries.Keys);
    }

    protected override Expression<Func<IndexEntry, string>> RetriveKey() => value => value.AbsoluteUri;

    protected override DbSet<IndexEntry> DbSetAccessor(FilesCacheContext ctx) => ctx.IndexEntries;

    protected override IndexEntry GetNew(string key, IDictionary<string, object>? param = null) =>
        GetNew(key, GetParam<string>(param, FileName));

    protected override void OnRetrievedNew(IndexEntry value)
    {
        base.OnRetrievedNew(value);
        value.Config = Config;
        value.Refresh();
    }

    private IndexEntry GetNew(string key, string? fileName = null) => new(key, Config, fileName);

    private async Task<int> RemoveIndexEntriesOfMissingFilesAsync(FilesCacheContext ctx, HashSet<string> existingFiles)
    {
        var entries = await DbSetAccessor(ctx)
            .AsNoTracking()
            .Select(x => new
            {
                x.AbsoluteUri,
                x.FilePath
            })
            .ToArrayAsync();

        IList<string> ids = new List<string>();

        foreach (var entry in entries)
        {
            if (entry.FilePath is null
                || !existingFiles.Contains(entry.FilePath))
            {
                ids.Add(entry.AbsoluteUri);
            }
            else
            {
                existingFiles.Remove(entry.FilePath);
                AddKeyToIndex(entry.AbsoluteUri);
            }
        }

        return await RemoveIndexEntriesAsync(ctx, ids);
    }

    // CS0618: BatchDeleteAsync (third-party EF batch extension) is obsolete in favour of the
    // EF7+ native ExecuteDelete. Migration tracked as tech debt; behavior retained for now.
#pragma warning disable CS0618
    private async Task<int> RemoveIndexEntriesAsync(FilesCacheContext ctx, ICollection<string> ids) =>
        ids.IsNotNullOrEmptyCollection()
            ? await DbSetAccessor(ctx).Where(x => ids.Contains(x.AbsoluteUri)).BatchDeleteAsync()
            : 0;
#pragma warning restore CS0618

    private Result<FileInfo, ExceptionError> SafeDeleteFile(FileInfo file)
    {
        var result = FileSystemUtilities.SafeDeleteFile(file);

        return result.IsSuccess
            ? file
            : result.Error!; // IsFailure guarantees Error is non-null
    }

    private Result<FileInfo, ExceptionError> SafeDeleteFile(string filePath) => SafeDeleteFile(new FileInfo(filePath));
}