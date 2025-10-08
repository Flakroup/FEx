using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.PersistentStorage.Abstractions;

public interface IFExCachedFile : IDisposable
{
    DateTime Timestamp { get; }
    bool IsExpired { get; }
    string Filename { get; }
    bool IsDownloading { get; set; }
    Uri Url { get; }
    string Id { get; }

    void UpdateData(IFExCachedFile data);
    MemoryStream GetDataStream();
    Task<MemoryStream> GetDataStreamAsync(CancellationToken cancellationToken);
}