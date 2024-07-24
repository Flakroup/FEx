using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.AzureStorage;

public interface IAzureStorageService
{
    void Configure(string connStr, int parallelOpsMultiplier = 8);

    Task<(CloudBlockBlob blob, bool isSuccess)> CopyBlobAsync(string containerName,
                                                              string srcBlob,
                                                              string destBlob,
                                                              bool overwrite = true,
                                                              Func<CloudBlob, Task> blobAction = null);

    Task<bool> DeleteBlobAsync(CloudBlockBlob blob,
                               DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None,
                               AccessCondition accessCondition = null,
                               BlobRequestOptions options = null,
                               OperationContext operationContext = null,
                               CancellationToken cancellationToken = default);

    Task<bool> DownloadLatestBlobsAsync(string downloadDir,
                                        string containerName,
                                        bool deleteOldFiles = true,
                                        string deleteFilesMask = "*.*",
                                        bool noDownload = false,
                                        params string[] paths);

    Task<bool> ExistsAsync(CloudBlockBlob blob,
                           bool primaryOnly = false,
                           BlobRequestOptions options = null,
                           OperationContext operationContext = null,
                           CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string containerName,
                           string path,
                           string fileName,
                           bool primaryOnly = false,
                           BlobRequestOptions options = null,
                           OperationContext operationContext = null,
                           CancellationToken cancellationToken = default);

    Task<CloudBlockBlobInfo> GetBlobAsync(string path,
                                          CloudBlobContainer container = null,
                                          string containerName = null,
                                          CancellationToken cancellationToken = default);

    Task<IList<T>> GetBlobsAsync<T>(string containerName, string path, bool useFlatBlobListing = false)
        where T : CloudBlob;

    CloudBlobContainer GetCloudBlobContainer(string containerName);

    Task<IList<CloudBlockBlob>> GetCloudBlockBlobsAsync(string containerName,
                                                        string path,
                                                        bool useFlatBlobListing = false);

    Task<IList<CloudBlockBlobInfo>> GetCloudBlockBlobsInfoAsync(string containerName,
                                                                string path,
                                                                bool useFlatBlobListing = false);

    Task<(string fileName, FileInfo localPath)> ProcessBlobAsync(string containerName, string downloadDir, string path);

    Task<(FileInfo file, CloudBlockBlobInfo blob)> UploadFileAsync(string path,
                                                                   bool overwrite,
                                                                   FileInfo file,
                                                                   string containerName = null,
                                                                   CloudBlobContainer container = null,
                                                                   CancellationToken cancellationToken = default);

    Task<IDictionary<FileInfo, CloudBlockBlobInfo>> UploadFilesAsync(string containerName,
                                                                     string path,
                                                                     bool overwrite = false,
                                                                     bool oneByOne = false,
                                                                     params FileInfo[] files);

    Task<(string file, CloudBlockBlob blob)> UploadStreamAsync(string path,
                                                               bool overwrite,
                                                               string fileName,
                                                               Stream stream,
                                                               string containerName = null,
                                                               CloudBlobContainer container = null);
}