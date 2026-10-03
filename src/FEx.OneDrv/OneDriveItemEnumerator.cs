using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using FEx.OneDrv.Models;
using Microsoft.Graph.Drives.Item.Items.Item.Children;
using Microsoft.Graph.Models;
using Polly;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv;

/// <summary>Microsoft Graph implementation of <see cref="IOneDriveItemEnumerator"/> that streams page by page.</summary>
public sealed class OneDriveItemEnumerator : IOneDriveItemEnumerator
{
    private readonly IGraphServiceClientCache _graphCache;
    private readonly IFExLogger _logger;
    private readonly ResiliencePipeline _pipeline;

    public OneDriveItemEnumerator(IGraphServiceClientCache graphCache, IFExLogger logger)
    {
        _graphCache = graphCache ?? throw new ArgumentNullException(nameof(graphCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipeline = GraphResiliencePipeline.Create(_logger);
    }

    public async IAsyncEnumerable<IOneDriveFile> EnumerateFilesAsync(string folderId,
                                                                     [EnumeratorCancellation]
                                                                     CancellationToken cancellationToken)
    {
        var (client, driveId) = await _graphCache.GetAsync(cancellationToken);

        var parentId = string.IsNullOrEmpty(folderId) || folderId == "root"
            ? "root"
            : folderId;

        var pipeline = _pipeline;
        var count = 0;

        // Fetch one page at a time (each fetch retried on its own) and yield its files before requesting the next
        // page, so memory stays bounded by the page size and a retry never re-yields items.
        var request = client.Drives[driveId].Items[parentId].Children;
        var response = await GetPageAsync(pipeline, request, null, cancellationToken);

        while (true)
        {
            foreach (var item in response.Value ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.File is null)
                    continue;

                count++;

                yield return DriveItemMapper.MapFile(item);
            }

            var nextLink = response.OdataNextLink;

            if (string.IsNullOrEmpty(nextLink))
                break;

            response = await GetPageAsync(pipeline, request, nextLink, cancellationToken);
        }

        _logger.Information($"Enumerated {count} items in folder {folderId ?? "root"}");
    }

    private static async Task<DriveItemCollectionResponse> GetPageAsync(ResiliencePipeline pipeline,
                                                                          ChildrenRequestBuilder request,
                                                                          string? nextLink,
                                                                          CancellationToken cancellationToken) =>
        await pipeline.ExecuteAsync(async cancelToken =>
            {
                var page = nextLink is null
                    ? await request.GetAsync(cancellationToken: cancelToken)
                    : await request.WithUrl(nextLink).GetAsync(cancellationToken: cancelToken);

                return page ?? throw new InvalidOperationException("Graph returned no response for the folder listing.");
            },
            cancellationToken);
}
