using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.LiteDbx.Abstractions.Interfaces;

public interface ICachedFile
{
    DateTime Timestamp { get; }
    bool IsExpired { get; }
    string Filename { get; }
    bool IsDownloading { get; set; }
    Uri Url { get; }

    void UpdateData(ICachedFile data);
    Stream GetDataStream();
    Task<Stream> GetDataStreamAsync(CancellationToken cancellationToken);
}