using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using FEx.OneDrv.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FEx.OneDrv;

public sealed class OneDriveItemEnumerator : IOneDriveItemEnumerator
{
    private readonly IGraphServiceClientCache _graphCache;
    private readonly IFExLogger _logger;

    public OneDriveItemEnumerator(IGraphServiceClientCache graphCache, IFExLogger logger)
    {
        _graphCache = graphCache ?? throw new ArgumentNullException(nameof(graphCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async IAsyncEnumerable<IOneDriveFile> EnumerateFilesAsync(string folderId,
                                                                     [EnumeratorCancellation]
                                                                     CancellationToken cancellationToken)
    {
        var (client, driveId) = await _graphCache.GetAsync(cancellationToken);

        var parentId = string.IsNullOrEmpty(folderId) || folderId == "root"
            ? "root"
            : folderId;

        var files = new List<IOneDriveFile>();
        var pipeline = GraphResiliencePipeline.Create(_logger);

        await pipeline.ExecuteAsync(async cancelToken =>
            {
                files.Clear();

                var response = await client.Drives[driveId]
                    .Items[parentId]
                    .Children.GetAsync(cancellationToken: cancelToken);

                if (response is null)
                    throw new InvalidOperationException("Graph returned no response for the folder listing.");

                var iterator = PageIterator<DriveItem, DriveItemCollectionResponse>.CreatePageIterator(client,
                    response,
                    item =>
                    {
                        if (item.File != null)
                            files.Add(DriveItemMapper.MapFile(item));

                        return true;
                    });

                await iterator.IterateAsync(cancelToken);
            },
            cancellationToken);

        _logger.Information($"Enumerated {files.Count} items in folder {folderId ?? "root"}");

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return file;
        }
    }
}