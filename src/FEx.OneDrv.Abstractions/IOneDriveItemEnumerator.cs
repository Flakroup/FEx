using System.Collections.Generic;
using System.Threading;

namespace FEx.OneDrv.Abstractions;

public interface IOneDriveItemEnumerator
{
    IAsyncEnumerable<IOneDriveFile> EnumerateFilesAsync(string folderId, CancellationToken cancellationToken);
}