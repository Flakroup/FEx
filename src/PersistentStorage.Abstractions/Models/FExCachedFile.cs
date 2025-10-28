using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.BaseObjects;
using FEx.PersistentStorage.Abstractions.Extensions;
using LiteDB;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.PersistentStorage.Abstractions.Models;

public class FExCachedFile : NotifyPropertyChanged, IFExCachedFile, IAsyncDisposable
{
    public const string UrlMetadataName = "originUrl";

    private readonly MemoryStream _dataStream;
    private bool _isDisposed;

    private bool _isDownloading;

    public string Filename { get; private set; }
    public DateTime Timestamp { get; private set; }
    public bool IsExpired => CachedFileExtensions.IsExpired(Timestamp);
    public Uri Url { get; }
    public string Id { get; }

    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetProperty(ref _isDownloading, value);
    }

    public FExCachedFile(LiteFileInfo<string> file)
    {
        Url = new(file.Metadata[UrlMetadataName].AsString);
        Id = CachedFileExtensions.GetFileId(Url);
        Timestamp = file.UploadDate.ToUniversalTime();
        Filename = file.Filename;
        using var liteFileStream = file.OpenRead();
        _dataStream = liteFileStream.CopyToMemoryStream();
    }

    public FExCachedFile(FileInfo file, Uri url)
    {
        Url = url;
        Id = CachedFileExtensions.GetFileId(Url);
        Timestamp = file.LastWriteTimeUtc;
        Filename = file.Name;
        using var fileStream = file.OpenRead();
        _dataStream = fileStream.CopyToMemoryStream();
    }

    public FExCachedFile(IFExDownloadResult downloadResult)
    {
        Timestamp = DateTime.UtcNow;
        Url = downloadResult.Url;
        Id = CachedFileExtensions.GetFileId(Url);
        Filename = downloadResult.FileName;
        _dataStream = downloadResult.UseDataStream(static stream => stream.CopyToMemoryStream());
    }

    public void UpdateData(IFExCachedFile data)
    {
        _dataStream.SetLength(0);
        data.GetDataStream().CopyTo(_dataStream);
        _dataStream.Seek(0, SeekOrigin.Begin);
        Timestamp = data.Timestamp;
        Filename = data.Filename;
    }

    public MemoryStream GetDataStream() => _dataStream.CopyToMemoryStream();

    public async Task<MemoryStream> GetDataStreamAsync(CancellationToken cancellationToken) =>
        await _dataStream.CopyToMemoryStreamAsync(false, cancellationToken);

    #region IDisposable
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            _dataStream?.Dispose();

        _isDisposed = true;
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
#if NETSTANDARD2_0
        {
            _dataStream?.Dispose();
            await Task.CompletedTask;
        }
#else
            await _dataStream.DisposeAsync();
#endif

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}