using System;

namespace FEx.OneDrv.Abstractions;

public interface IOneDriveFile
{
    string Id { get; }
    string Name { get; }
    string Path { get; }
    long? Size { get; }
    string MimeType { get; }
    string QuickXorHash { get; }
    string Sha256Hash { get; }
    DateTimeOffset? LastModified { get; }
    string CreatedBy { get; }
    int? ImageWidth { get; }
    int? ImageHeight { get; }
    int? VideoDurationMs { get; }
}