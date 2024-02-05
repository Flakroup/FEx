using FEx.Downloader.Enums;
using System.IO;

namespace FEx.Downloader.Abstractions.Interfaces;

public interface IDownloadChunk : IDownloadPart
{
    string DirPath { get; }
    FileInfo File { get; }
    FileStream FileStream { get; }
    bool IsFileStreamOpen { get; set; }
    DownloadState State { get; set; }

    bool CheckIfChunkFileIsFinished();
}