using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.BaseObjects;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader;

public sealed class DownloadChunk : NotifyPropertyChanged, IDownloadChunk, IDisposable
{
    private long _size;
    private FileStream? _fileStream;
    private bool _isFileStreamOpen;

    public string RangeHeader { get; }
    public ContentRangeHeaderValue RangeHeaderValue { get; }
    public long From { get; }
    public long To { get; }
    public string DirPath { get; }
    public DownloadState State { get; set; }
    public FileInfo File { get; }

    public long ExpectedSize { get; }

    public long Size
    {
        get => _size;
        private set
        {
            if (SetProperty(ref _size, value))
            {
                if (Size > 0
                    && Size < ExpectedSize)
                {
                    State = DownloadState.InProgress;
                }
                else if (Size == ExpectedSize)
                {
                    State = DownloadState.Finished;
                    FixChunkFileSize();
                }
            }
        }
    }

    public FileStream FileStream
    {
        get
        {
            if (_fileStream is null)
                SetFileStream(File.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));

            // Lazy-initialized above (SetFileStream assigns a non-null stream), so it is non-null here.
            return _fileStream!;
        }
    }

    public bool IsFileStreamOpen
    {
        get => _isFileStreamOpen;
        set => SetProperty(ref _isFileStreamOpen, value);
    }

    private IProgress<double>? Progress { get; }

    public DownloadChunk(long from, long to, DirectoryInfo directory, IProgress<double>? progress)
        : this(new ContentRangeHeaderValue(from, to), directory, progress)
    {
    }

    public DownloadChunk(ContentRangeHeaderValue rangeHeader, DirectoryInfo directory, IProgress<double>? progress)
    {
        Progress = progress;
        RangeHeaderValue = rangeHeader;
        RangeHeader = RangeHeaderValue.ToString();
        From = RangeHeaderValue.From ?? throw new ArgumentNullException(nameof(ContentRangeHeaderValue.From));
        To = RangeHeaderValue.To ?? throw new ArgumentNullException(nameof(ContentRangeHeaderValue.To));

        ExpectedSize = To - From + 1;

        DirPath = directory.FullName;
        File = directory.GetDescendantFile($"{From}.part");

        if (!CheckIfChunkFileIsFinished()
            && File.Exists)
            FixChunkFileSize();
    }

    public DownloadChunk(string rangeHeader, DirectoryInfo directory, IProgress<double>? progress)
        : this(rangeHeader.GetContentRange().Guard(nameof(rangeHeader)), directory, progress)
    {
    }

    public bool CheckIfChunkFileIsFinished()
    {
        if (!File.Exists
            || File.Length != ExpectedSize)
            return false;

        Size = File.Length;

        return true;
    }

    public void ClearChunk()
    {
        Size = 0;
        FixChunkFileSize();
    }

    public Task WriteBytesAsync(byte[] buffer, int offset, int bytesToWrite) =>
        WriteBytesAsync(buffer, offset, bytesToWrite, CancellationToken.None);

    public async Task WriteBytesAsync(byte[] buffer, int offset, int bytesToWrite, CancellationToken token)
    {
        FileStream.Seek(Size, SeekOrigin.Begin);
        await FileStream.WriteAsync(buffer, offset, bytesToWrite, token);
        Size += bytesToWrite;
        Progress?.Report(bytesToWrite);
    }

    private void FixChunkFileSize()
    {
        CloseFileStream();

        File.Refresh();

        if (File.Exists)
            using (var fileStream = File.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                fileStream.SetLength(Size);
    }

    private void CloseFileStream()
    {
        if (IsFileStreamOpen)
        {
            _fileStream?.Flush();
            _fileStream?.Close();
            _fileStream?.Dispose();
            SetFileStream(null);
        }
    }

    private void SetFileStream(FileStream? value)
    {
        if (SetProperty(ref _fileStream, value))
            IsFileStreamOpen = _fileStream is not null;
    }

    #region IDisposable
    public void Dispose()
    {
        CloseFileStream();
    }
    #endregion
}