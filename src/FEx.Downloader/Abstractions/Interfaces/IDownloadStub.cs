namespace FEx.Downloader.Abstractions.Interfaces;

public interface IDownloadStub : IDownloadBase
{
    string MD5Checksum { get; }
    int ParallelRanges { get; }
    long DataLength { get; }
}