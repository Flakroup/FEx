using System.Collections.Generic;
using System.Threading;

namespace FEx.OneDrv.Abstractions;

/// <summary>Streams OneDrive items lazily.</summary>
public interface IOneDriveItemEnumerator
{
    /// <summary>Yields the files of a folder page by page; the next page is requested only after the current one is consumed.</summary>
    IAsyncEnumerable<IOneDriveFile> EnumerateFilesAsync(string folderId, CancellationToken cancellationToken);
}