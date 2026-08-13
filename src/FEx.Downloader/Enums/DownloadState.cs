namespace FEx.Downloader.Enums;

public enum DownloadState
{
    None,
    Connecting,
    InProgress,
    Finished,
    Failed,
    TargetCreation,
    Cleanup,
    Cancelled,
    ChecksumMismatch
}