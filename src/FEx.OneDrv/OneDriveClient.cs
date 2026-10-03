using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using FEx.OneDrv.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv;

/// <summary>Microsoft Graph implementation of <see cref="IOneDriveClient"/> with retry on transient failures.</summary>
public sealed class OneDriveClient : IOneDriveClient
{
    private readonly IGraphServiceClientCache _graphCache;
    private readonly IFExLogger _logger;
    private readonly ResiliencePipeline _pipeline;

    public OneDriveClient(IGraphServiceClientCache graphCache, IFExLogger logger)
    {
        _graphCache = graphCache ?? throw new ArgumentNullException(nameof(graphCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipeline = GraphResiliencePipeline.Create(_logger);
    }

    public async Task<IList<IOneDriveFile>> ListFilesAsync(string folderId, CancellationToken cancellationToken)
    {
        var (client, driveId) = await _graphCache.GetAsync(cancellationToken);

        var parentId = string.IsNullOrEmpty(folderId) || folderId == "root"
            ? "root"
            : folderId;

        var result = new List<IOneDriveFile>();
        var pipeline = _pipeline;

        await pipeline.ExecuteAsync(async cancelToken =>
            {
                result.Clear();

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
                            result.Add(DriveItemMapper.MapFile(item));

                        return true;
                    });

                await iterator.IterateAsync(cancelToken);
            },
            cancellationToken);

        _logger.Information($"Listed {result.Count} files in folder {folderId ?? "root"}");

        return result;
    }

    public async Task<IOneDriveFile> GetFileAsync(string itemId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentNullException(nameof(itemId));

        var (client, driveId) = await _graphCache.GetAsync(cancellationToken);
        var pipeline = _pipeline;

        var item = await pipeline.ExecuteAsync(async cancelToken =>
                await client.Drives[driveId].Items[itemId].GetAsync(cancellationToken: cancelToken),
            cancellationToken);

        if (item is null)
            throw new InvalidOperationException($"Drive item '{itemId}' was not found.");

        _logger.Information($"Retrieved file {itemId}");

        return DriveItemMapper.MapFile(item);
    }

    public async Task<IList<IOneDriveFolder>> ListFoldersAsync(string folderId, CancellationToken cancellationToken)
    {
        var (client, driveId) = await _graphCache.GetAsync(cancellationToken);

        var parentId = string.IsNullOrEmpty(folderId) || folderId == "root"
            ? "root"
            : folderId;

        var result = new List<IOneDriveFolder>();
        var pipeline = _pipeline;

        await pipeline.ExecuteAsync(async cancelToken =>
            {
                result.Clear();

                var response = await client.Drives[driveId]
                    .Items[parentId]
                    .Children.GetAsync(cancellationToken: cancelToken);

                if (response is null)
                    throw new InvalidOperationException("Graph returned no response for the folder listing.");

                var iterator = PageIterator<DriveItem, DriveItemCollectionResponse>.CreatePageIterator(client,
                    response,
                    item =>
                    {
                        if (item.Folder != null)
                            result.Add(DriveItemMapper.MapFolder(item));

                        return true;
                    });

                await iterator.IterateAsync(cancelToken);
            },
            cancellationToken);

        _logger.Information($"Listed {result.Count} folders in folder {folderId ?? "root"}");

        return result;
    }
}