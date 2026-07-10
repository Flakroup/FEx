using FEx.OneDrv.Abstractions;
using Microsoft.Graph.Models;
using System;

namespace FEx.OneDrv.Models;

internal static class DriveItemMapper
{
    internal static IOneDriveFile MapFile(DriveItem item)
    {
        if (item.Id is null)
            throw new InvalidOperationException("DriveItem is missing an Id.");
        if (item.Name is null)
            throw new InvalidOperationException("DriveItem is missing a Name.");

        return new OneDriveFile
        {
            Id = item.Id,
            Name = item.Name,
            Path = item.ParentReference?.Path,
            Size = item.Size,
            MimeType = item.File?.MimeType,
            QuickXorHash = item.File?.Hashes?.QuickXorHash,
            Sha256Hash = item.File?.Hashes?.Sha256Hash,
            LastModified = item.LastModifiedDateTime,
            CreatedBy = item.CreatedBy?.User?.DisplayName,
            ImageWidth = item.Image?.Width,
            ImageHeight = item.Image?.Height,
            VideoDurationMs = item.Video?.Duration.HasValue == true
                ? (int?)item.Video.Duration.Value
                : null
        };
    }

    internal static IOneDriveFolder MapFolder(DriveItem item)
    {
        if (item.Id is null)
            throw new InvalidOperationException("DriveItem is missing an Id.");
        if (item.Name is null)
            throw new InvalidOperationException("DriveItem is missing a Name.");

        return new OneDriveFolder
        {
            Id = item.Id,
            Name = item.Name,
            Path = item.ParentReference?.Path,
            ChildCount = item.Folder?.ChildCount
        };
    }
}