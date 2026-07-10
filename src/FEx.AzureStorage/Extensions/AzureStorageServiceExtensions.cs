using Microsoft.Azure.Storage.Blob;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.AzureStorage.Extensions;

public static class AzureStorageServiceExtensions
{
    public static void Configure(this IAzureStorageService service, string connStr) => service.Configure(connStr, 8);

    public static Task<(CloudBlockBlob? blob, bool isSuccess)> CopyBlobAsync(
        this IAzureStorageService service,
        string containerName,
        string srcBlob,
        string destBlob) =>
        service.CopyBlobAsync(containerName, srcBlob, destBlob, true, null);

    public static Task<bool> DeleteBlobAsync(this IAzureStorageService service, CloudBlockBlob blob) =>
        service.DeleteBlobAsync(blob, DeleteSnapshotsOption.None, null, null, null, CancellationToken.None);

    public static Task<bool> DownloadLatestBlobsAsync(this IAzureStorageService service,
                                                      string downloadDir,
                                                      string containerName,
                                                      params string[] paths) =>
        service.DownloadLatestBlobsAsync(downloadDir, containerName, true, "*.*", false, paths);

    public static Task<bool> ExistsAsync(this IAzureStorageService service, CloudBlockBlob blob) =>
        service.ExistsAsync(blob, false, null, null, CancellationToken.None);

    public static Task<bool> ExistsAsync(this IAzureStorageService service,
                                         string containerName,
                                         string path,
                                         string fileName) =>
        service.ExistsAsync(containerName, path, fileName, false, null, null, CancellationToken.None);

    public static Task<CloudBlockBlobInfo> GetBlobAsync(this IAzureStorageService service, string path) =>
        service.GetBlobAsync(path, null, null, CancellationToken.None);

    public static Task<IList<T>> GetBlobsAsync<T>(this IAzureStorageService service,
                                                  string containerName,
                                                  string path,
                                                  CancellationToken cancellationToken)
        where T : CloudBlob =>
        service.GetBlobsAsync<T>(containerName, path, false, cancellationToken);

    public static Task<IList<CloudBlockBlob>> GetCloudBlockBlobsAsync(this IAzureStorageService service,
                                                                      string containerName,
                                                                      string path,
                                                                      CancellationToken cancellationToken) =>
        service.GetCloudBlockBlobsAsync(containerName, path, false, cancellationToken);

    public static Task<IList<CloudBlockBlobInfo>> GetCloudBlockBlobsInfoAsync(
        this IAzureStorageService service,
        string containerName,
        string path,
        CancellationToken cancellationToken) =>
        service.GetCloudBlockBlobsInfoAsync(containerName, path, false, cancellationToken);

    public static Task<(FileInfo file, CloudBlockBlobInfo blob)> UploadFileAsync(
        this IAzureStorageService service,
        string path,
        bool overwrite,
        FileInfo file) =>
        service.UploadFileAsync(path, overwrite, file, null, null, CancellationToken.None);

    public static Task<IDictionary<FileInfo, CloudBlockBlobInfo>> UploadFilesAsync(
        this IAzureStorageService service,
        string containerName,
        string path,
        params FileInfo[] files) =>
        service.UploadFilesAsync(containerName, path, false, false, files);

    public static Task<(string file, CloudBlockBlob blob)> UploadStreamAsync(
        this IAzureStorageService service,
        string path,
        bool overwrite,
        string fileName,
        Stream stream) =>
        service.UploadStreamAsync(path, overwrite, fileName, stream, null, null);
}