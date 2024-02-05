using FEx.Basics.Abstractions;
using FEx.Extensions.IO;
using FEx.LiteDBx.Abstractions.Interfaces;
using LiteDB;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.LiteDBx.Models;

public sealed class CachedFile : NotifyPropertyChanged, ICachedFile, IDisposable
#if NET
    , IAsyncDisposable
#endif
{
    private readonly MemoryStream _dataStream;

    private bool _isDownloading;

    public static TimeSpan ExpirationTimeSpan { get; set; } = TimeSpan.FromHours(24);

    public DateTime Timestamp { get; private set; }
    public bool IsExpired => Timestamp.Add(ExpirationTimeSpan) <= DateTime.UtcNow;

    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetProperty(ref _isDownloading, value);
    }

    public string Filename { get; private set; }

    public Uri Url { get; }

    public CachedFile(LiteFileInfo<string> file, Uri url)
    {
        Url = url;
        _dataStream = new MemoryStream();
        file.CopyTo(_dataStream);
        Timestamp = file.UploadDate.ToUniversalTime();
        Filename = file.Filename;
    }

    public CachedFile(FileInfo file, Uri url)
    {
        Url = url;
        using FileStream fileStream = file.OpenRead();
        _dataStream = new MemoryStream();
        fileStream.CopyTo(_dataStream);
        Timestamp = file.LastWriteTimeUtc;
        Filename = file.Name;
    }

    public CachedFile(IDownloadResult downloadResult)
    {
        Timestamp = DateTime.UtcNow;
        Url = downloadResult.Url;
        _dataStream = new MemoryStream();
        downloadResult.UseDataStream(stream => stream.CopyTo(_dataStream));
        Filename = downloadResult.FileName;
    }

    public void UpdateData(ICachedFile data)
    {
        _dataStream.SetLength(0);
        data.GetDataStream().CopyTo(_dataStream);
        Timestamp = data.Timestamp;
        Filename = data.Filename;
    }

    public Stream GetDataStream() => _dataStream.CopyToMemoryStream();

    public async Task<Stream> GetDataStreamAsync(CancellationToken cancellationToken) =>
        await _dataStream.CopyToMemoryStreamAsync(cancellationToken: cancellationToken);

    #region IDisposable
    public void Dispose()
    {
        _dataStream?.Dispose();
    }

#if NET
    public async ValueTask DisposeAsync()
    {
        if (_dataStream != null)
            await _dataStream.DisposeAsync();
    }
#endif
    #endregion
}