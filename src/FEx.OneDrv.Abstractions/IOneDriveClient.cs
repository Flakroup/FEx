using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Abstractions;

public interface IOneDriveClient
{
    Task<IList<IOneDriveFile>> ListFilesAsync(string folderId, CancellationToken cancellationToken);
    Task<IOneDriveFile> GetFileAsync(string itemId, CancellationToken cancellationToken);
    Task<IList<IOneDriveFolder>> ListFoldersAsync(string folderId, CancellationToken cancellationToken);
}
