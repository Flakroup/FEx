using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using FEx.OneDrv.Auth;
using FEx.OneDrv.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FEx.OneDrv;

public sealed class OneDriveItemEnumerator : IOneDriveItemEnumerator
{
    private readonly IOneDriveAuthService _authService;
    private readonly IFExLogger _logger;

    public OneDriveItemEnumerator(IOneDriveAuthService authService, IFExLogger logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async IAsyncEnumerable<IOneDriveFile> EnumerateFilesAsync(
        string folderId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = await GraphClientHelper.CreateAsync(_authService, cancellationToken);
        var driveId = await GraphClientHelper.GetDefaultDriveIdAsync(client, cancellationToken);
        var buffer = new ConcurrentQueue<IOneDriveFile>();

        var parentId = string.IsNullOrEmpty(folderId) || folderId == "root" ? "root" : folderId;
        var response = await client.Drives[driveId].Items[parentId].Children.GetAsync(cancellationToken: cancellationToken);

        var pageIterator = PageIterator<DriveItem, DriveItemCollectionResponse>.CreatePageIterator(
            client, response,
            item =>
            {
                if (item.File != null)
                    buffer.Enqueue(DriveItemMapper.MapFile(item));
                return true;
            });

        await pageIterator.IterateAsync(cancellationToken);

        while (buffer.TryDequeue(out var file))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return file;
        }

        _logger.Information($"Enumerated items in folder {folderId ?? "root"}");
    }
}
