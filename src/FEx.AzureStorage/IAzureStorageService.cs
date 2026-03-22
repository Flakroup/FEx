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
    void Configure(string connStr, int parallelOpsMultiplier);

    Task<(CloudBlockBlob blob, bool isSuccess)> CopyBlobAsync(string containerName,
                                                              string srcBlob,
                                                              string destBlob,
                                                              bool overwrite,
                                                              Func<CloudBlob, Task> blobAction);

    Task<bool> DeleteBlobAsync(CloudBlockBlob blob,
                               DeleteSnapshotsOption deleteSnapshotsOption,
                               AccessCondition accessCondition,
                               BlobRequestOptions options,
                               OperationContext operationContext,
                               CancellationToken cancellationToken);

    Task<bool> DownloadLatestBlobsAsync(string downloadDir,
                                        string containerName,
                                        bool deleteOldFiles,
                                        string deleteFilesMask,
                                        bool noDownload,
                                        params string[] paths);

    Task<bool> ExistsAsync(CloudBlockBlob blob,
                           bool primaryOnly,
                           BlobRequestOptions options,
                           OperationContext operationContext,
                           CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string containerName,
                           string path,
                           string fileName,
                           bool primaryOnly,
                           BlobRequestOptions options,
                           OperationContext operationContext,
                           CancellationToken cancellationToken);

    Task<CloudBlockBlobInfo> GetBlobAsync(string path,
                                          CloudBlobContainer container,
                                          string containerName,
                                          CancellationToken cancellationToken);

    Task<IList<T>> GetBlobsAsync<T>(string containerName, string path, bool useFlatBlobListing)
        where T : CloudBlob;

    CloudBlobContainer GetCloudBlobContainer(string containerName);

    Task<IList<CloudBlockBlob>> GetCloudBlockBlobsAsync(string containerName,
                                                        string path,
                                                        bool useFlatBlobListing);

    Task<IList<CloudBlockBlobInfo>> GetCloudBlockBlobsInfoAsync(string containerName,
                                                                string path,
                                                                bool useFlatBlobListing);

    Task<(string fileName, FileInfo localPath)> ProcessBlobAsync(string containerName, string downloadDir, string path);

    Task<(FileInfo file, CloudBlockBlobInfo blob)> UploadFileAsync(string path,
                                                                   bool overwrite,
                                                                   FileInfo file,
                                                                   string containerName,
                                                                   CloudBlobContainer container,
                                                                   CancellationToken cancellationToken);

    Task<IDictionary<FileInfo, CloudBlockBlobInfo>> UploadFilesAsync(string containerName,
                                                                     string path,
                                                                     bool overwrite,
                                                                     bool oneByOne,
                                                                     params FileInfo[] files);

    Task<(string file, CloudBlockBlob blob)> UploadStreamAsync(string path,
                                                               bool overwrite,
                                                               string fileName,
                                                               Stream stream,
                                                               string containerName,
                                                               CloudBlobContainer container);
}
