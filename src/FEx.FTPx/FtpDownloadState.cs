namespace FEx.FTPx;

/// <summary>
/// Retry bookkeeping for one download. Never share an instance between concurrent downloads.
/// </summary>
public sealed class FtpDownloadState
{
    internal const int MaxReadRetries = 10;

    /// <summary>Consecutive aborted reads since the last successful read.</summary>
    public int RetryCount { get; internal set; }
}
