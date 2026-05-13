using FEx.OneDrv.Abstractions;
using System;

namespace FEx.OneDrv.Models;

internal sealed class OneDriveFile : IOneDriveFile
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Path { get; init; }
    public long? Size { get; init; }
    public string MimeType { get; init; }
    public string QuickXorHash { get; init; }
    public string Sha256Hash { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public string CreatedBy { get; init; }
    public int? ImageWidth { get; init; }
    public int? ImageHeight { get; init; }
    public int? VideoDurationMs { get; init; }
}
