using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv.Abstractions;

/// <summary>Lists and reads OneDrive files and folders.</summary>
public interface IOneDriveClient
{
    /// <summary>Lists the files (not folders) directly inside a folder; <c>root</c> or empty means the drive root.</summary>
    Task<IList<IOneDriveFile>> ListFilesAsync(string folderId, CancellationToken cancellationToken);
    /// <summary>Gets a single file by item ID.</summary>
    Task<IOneDriveFile> GetFileAsync(string itemId, CancellationToken cancellationToken);
    /// <summary>Lists the folders directly inside a folder; <c>root</c> or empty means the drive root.</summary>
    Task<IList<IOneDriveFolder>> ListFoldersAsync(string folderId, CancellationToken cancellationToken);
}