using FEx.OneDrv.Abstractions;
using Microsoft.Graph.Models;

namespace FEx.OneDrv.Models;

internal static class DriveItemMapper
{
    internal static IOneDriveFile MapFile(DriveItem item) =>
        new OneDriveFile
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

    internal static IOneDriveFolder MapFolder(DriveItem item) =>
        new OneDriveFolder
        {
            Id = item.Id,
            Name = item.Name,
            Path = item.ParentReference?.Path,
            ChildCount = item.Folder?.ChildCount
        };
}