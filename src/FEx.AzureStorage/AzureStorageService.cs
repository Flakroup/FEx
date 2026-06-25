#if !NET5_0_OR_GREATER
using System.Net;
#endif
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.AzureStorage.Extensions;
using FEx.Json.Extensions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeleteSnapshotsOption = Microsoft.Azure.Storage.Blob.DeleteSnapshotsOption;

namespace FEx.AzureStorage;

public class AzureStorageService : IAzureStorageService
{
    /// <summary>
    /// The latest version according to
    /// https://docs.microsoft.com/en-us/rest/api/storageservices/versioning-for-the-azure-storage-services
    /// </summary>
    public const string LatestVersion = "2021-04-10";

    private readonly SemaphoreSlim _md5Semaphore;

    protected Progress<string> ProgressReporter { get; }
    protected string ConnStr { get; private set; }
    protected ConcurrentDictionary<string, ProgressState> ProgressStates { get; }
    protected ILogger<AzureStorageService> Log { get; }

    public AzureStorageService(ILogger<AzureStorageService> logger)
    {
        Log = logger.Guard(nameof(logger));

        ProgressStates = new();
        ProgressReporter = new(ReportProgress);
        _md5Semaphore = new(1, 1);
    }

    public void Configure(string connStr, int parallelOperationsPerProcessorCount)
    {
        var parallelOperationsCount = Environment.ProcessorCount * parallelOperationsPerProcessorCount;
        ConnStr = connStr;

        // SYSLIB0014: ServicePointManager is obsolete on net5+ (no-op for HttpClient); it still
        // tunes the connection pool on .NET Framework / netstandard, so it is compiled only there
        // (the using System.Net is guarded by the same condition).
#if !NET5_0_OR_GREATER
        ServicePointManager.Expect100Continue = false;
        ServicePointManager.DefaultConnectionLimit = parallelOperationsCount;
#endif
        TransferManager.Configurations.ParallelOperations = parallelOperationsCount;
        EnsureDefaultServiceVersion(ConnStr);
    }

    public async Task<(string fileName, FileInfo localPath)> ProcessBlobAsync(
        string containerName,
        string downloadDir,
        string path)
    {
        Log.LogInformation($"Preparing blob for container {containerName} and path {path}");

        var containerClient = await GetBlobContainerClientAsync(containerName);

        var blob = await containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, path, CancellationToken.None)
            .OrderByDescending(x => x.Properties.LastModified)
            .FirstOrDefaultAsync();

        var fileName = blob.Name;
        var localFile = new FileInfo(Path.Combine(downloadDir, Path.GetFileName(fileName)));

        return (fileName, localFile);
    }

    public CloudBlobContainer GetCloudBlobContainer(string containerName)
    {
        var account = CloudStorageAccount.Parse(ConnStr);
        var blobClient = account.CreateCloudBlobClient();

        return blobClient.GetContainerReference(containerName);
    }

    public async Task<bool> DownloadLatestBlobsAsync(string downloadDir,
                                                     string containerName,
                                                     bool deleteOldFiles,
                                                     string deleteFilesMask,
                                                     bool noDownload,
                                                     params string[] paths)
    {
        Directory.CreateDirectory(downloadDir);

        (string fileName, FileInfo localFile)[] blobsInfo =
            await paths.WithWhenAllTasksAsync(path => ProcessBlobAsync(containerName, downloadDir, path));

        if (deleteOldFiles)
            DeleteOldFiles(downloadDir, deleteFilesMask, [.. blobsInfo.Select(x => x.localFile.FullName)]);

        var preparedBlobs = await blobsInfo.WithWhenAllTasksAsync(x =>
            PrepareBlobDownloadAsync(containerName, x.fileName, x.localFile, noDownload));

        if (!noDownload)
            await preparedBlobs.WithWhenAllTasksAsync(x =>
                RunBlobDownloadAsync(x.localFile, x.sourceBlob, x.shouldBeDownloaded));

        IDictionary<string, string> resDictionary = new Dictionary<string, string>();
        IDictionary<string, string> invalidDownloads = new Dictionary<string, string>();

        foreach (var (localFile, sourceBlob, _) in preparedBlobs)
        {
            var path = string.Join("/", Path.GetDirectoryName(sourceBlob.Name).Split('\\'));
            localFile.Refresh();

            if (localFile.Exists)
            {
                resDictionary.Add(path, localFile.FullName);
            }
            else
            {
                resDictionary.Add(path, null);
                invalidDownloads.Add(path, localFile.FullName);
            }
        }

        SaveResults(resDictionary);

        if (invalidDownloads.IsNotNullOrEmptyCollection())
        {
            var msg = string.Join("\n", invalidDownloads.Select(x => $"{x.Key}: {x.Value}"));

            throw new($"Some blobs weren't successfully downloaded:\n{msg}");
        }

        return true;
    }

    public async Task<(string file, CloudBlockBlob blob)> UploadStreamAsync(
        string path,
        bool overwrite,
        string fileName,
        Stream stream,
        string containerName,
        CloudBlobContainer container)
    {
        container = container ?? GetCloudBlobContainer(containerName);
        var blobName = GetBlobName(path, fileName);
        Log.LogInformation($"Preparing blob for container {containerName} and path {blobName}");
        var destBlob = container.GetBlockBlobReference(blobName);
        var state = GetBlobProgressState(blobName, StorageOperation.Upload);
        state.Reset(blobName, Convert.ToDouble(stream.Length), null);

        var context = new SingleTransferContext
        {
            ProgressHandler = new Progress<TransferStatus>(prg => LogProgress(prg, state)),
            ShouldOverwriteCallbackAsync = (_, _) => Task.FromResult(overwrite)
        };

        // Upload a local blob
        await TransferManager.UploadAsync(stream, destBlob, null, context, CancellationToken.None);
        await destBlob.FetchAttributesAsync();

        return (fileName, destBlob);
    }

    public async Task<CloudBlockBlobInfo> GetBlobAsync(string path,
                                                       CloudBlobContainer container,
                                                       string containerName,
                                                       CancellationToken cancellationToken)
    {
        container = container ?? GetCloudBlobContainer(containerName);
        var destBlob = new CloudBlockBlobInfo(container.GetBlockBlobReference(path));

        if (await destBlob.EnsureExistsAsync(false, null, null, cancellationToken))
            await destBlob.FetchAttributesAsync(null, null, null, cancellationToken);

        return destBlob;
    }

    public async Task<IList<CloudBlockBlob>> GetCloudBlockBlobsAsync(string containerName,
                                                                     string path,
                                                                     bool useFlatBlobListing,
                                                                     CancellationToken cancellationToken) =>
        await GetBlobsAsync<CloudBlockBlob>(containerName, path, useFlatBlobListing, cancellationToken);

    public async Task<IList<T>> GetBlobsAsync<T>(string containerName,
                                                 string path,
                                                 bool useFlatBlobListing,
                                                 CancellationToken cancellationToken) where T : CloudBlob
    {
        var container = GetCloudBlobContainer(containerName);

        var result = await container.ListBlobsAsync(path,
            useFlatBlobListing,
            BlobListingDetails.Metadata,
            null,
            null,
            cancellationToken);

        var blobs = result.Data.Cast<T>().ToArray();

        await blobs.WithWhenAllTasksAsync(x => x.FetchAttributesAsync(cancellationToken));

        return blobs;
    }

    public async Task<bool> ExistsAsync(string containerName,
                                        string path,
                                        string fileName,
                                        bool primaryOnly,
                                        BlobRequestOptions options,
                                        OperationContext operationContext,
                                        CancellationToken cancellationToken)
    {
        var container = GetCloudBlobContainer(containerName);
        var blobName = $"{path}/{fileName}";
        var blob = container.GetBlockBlobReference(blobName);

        return await ExistsAsync(blob, primaryOnly, options, operationContext, cancellationToken);
    }

    public async Task<bool> ExistsAsync(CloudBlockBlob blob,
                                        bool primaryOnly,
                                        BlobRequestOptions options,
                                        OperationContext operationContext,
                                        CancellationToken cancellationToken)
    {
        if (cancellationToken == CancellationToken.None)
            cancellationToken = CancellationToken.None;

        return await blob.ExistsAsync(primaryOnly, options, operationContext, cancellationToken);
    }

    public async Task<(CloudBlockBlob blob, bool isSuccess)> CopyBlobAsync(
        string containerName,
        string srcBlob,
        string destBlob,
        bool overwrite,
        Func<CloudBlob, Task> blobAction)
    {
        var container = GetCloudBlobContainer(containerName);
        var sourceBlob = container.GetBlockBlobReference(srcBlob);

        if (await sourceBlob.ExistsAsync())
        {
            await sourceBlob.FetchAttributesAsync();
            var destinationBlob = container.GetBlockBlobReference(destBlob);

            var state = GetBlobProgressState(destBlob, StorageOperation.Upload);
            state.Reset(destBlob, Convert.ToDouble(sourceBlob.Properties.Length), null);

            var context = new SingleTransferContext
            {
                ProgressHandler = new Progress<TransferStatus>(prg => LogProgress(prg, state)),
                ShouldOverwriteCallbackAsync = (_, _) => Task.FromResult(overwrite)
            };

            await TransferManager.CopyAsync(sourceBlob,
                destinationBlob,
                CopyMethod.ServiceSideAsyncCopy,
                null,
                context);

            if (blobAction is not null)
                await blobAction(destinationBlob);

            return (destinationBlob, true);
        }

        return (null, false);
    }

    public async Task<IDictionary<FileInfo, CloudBlockBlobInfo>> UploadFilesAsync(
        string containerName,
        string path,
        bool overwrite,
        bool oneByOne,
        params FileInfo[] files)
    {
        if (files.Length == 0)
            throw new("No files to upload");

        var container = GetCloudBlobContainer(containerName);

        if (!oneByOne)
            return (await files.WithWhenAllTasksAsync(file =>
                UploadFileAsync(path, overwrite, file, containerName, container, CancellationToken.None))).ToDictionary(
                x => x.file,
                x => x.blob);

        var result = new Dictionary<FileInfo, CloudBlockBlobInfo>();

        foreach (var f in files)
        {
            var (file, destBlob) =
                await UploadFileAsync(path, overwrite, f, containerName, container, CancellationToken.None);

            result.Add(file, destBlob);
        }

        return result;
    }

    public async Task<(FileInfo file, CloudBlockBlobInfo blob)> UploadFileAsync(
        string path,
        bool overwrite,
        FileInfo file,
        string containerName,
        CloudBlobContainer container,
        CancellationToken cancellationToken)
    {
        container ??= GetCloudBlobContainer(containerName);
        var blobName = GetBlobName(path, file.Name);
        Log.LogInformation($"Preparing blob for container {containerName} and path {blobName}");
        var destBlob = container.GetBlockBlobReference(blobName);
        var state = GetBlobProgressState(blobName, StorageOperation.Upload);
        state.Reset(blobName, Convert.ToDouble(file.Length), null);

        var context = new SingleTransferContext
        {
            ProgressHandler = new Progress<TransferStatus>(prg => LogProgress(prg, state)),
            ShouldOverwriteCallbackAsync = (_, _) => Task.FromResult(overwrite)
        };

        await TransferManager.UploadAsync(file.FullName, destBlob, null, context, cancellationToken);
        await destBlob.FetchAttributesAsync(cancellationToken);
        var blob = new CloudBlockBlobInfo(destBlob, true);
        await blob.EnsureCorrectContentTypeAsync();

        return (file, blob);
    }

    public async Task<bool> DeleteBlobAsync(CloudBlockBlob blob,
                                            DeleteSnapshotsOption deleteSnapshotsOption,
                                            AccessCondition accessCondition,
                                            BlobRequestOptions options,
                                            OperationContext operationContext,
                                            CancellationToken cancellationToken) =>
        await blob.DeleteIfExistsAsync(deleteSnapshotsOption,
            accessCondition,
            options,
            operationContext,
            cancellationToken);

    public static void EnsureDefaultServiceVersion(string connectionString)
    {
        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var blobClient = storageAccount.CreateCloudBlobClient();
        var props = blobClient.GetServiceProperties();

        if (props.DefaultServiceVersion == null) //todo - or earlier if configuration allows 
        {
            props.DefaultServiceVersion = LatestVersion;
            blobClient.SetServiceProperties(props);
        }
    }

    public async Task<IList<CloudBlockBlobInfo>> GetCloudBlockBlobsInfoAsync(
        string containerName,
        string path,
        bool useFlatBlobListing,
        CancellationToken cancellationToken) =>
        (await GetBlobsAsync<CloudBlockBlob>(containerName, path, useFlatBlobListing, cancellationToken)).AsParallel()
        .Select(x => new CloudBlockBlobInfo(x))
        .ToArray();

    private static string GetBlobName(string path, string fileName) =>
        path.IsNotNullOrEmptyString()
            ? $"{path}/{fileName}"
            : fileName;

    private static void LogProgress(TransferStatus progress, ProgressState state) => state.LogProgress(progress);

    private static void DeleteOldFiles(string downloadDir, string deleteFilesMask, params string[] except)
    {
        var files = new DirectoryInfo(downloadDir).EnumerateFiles(deleteFilesMask)
            .Where(x => except?.Contains(x.FullName) != true)
            .ToArray();

        var exceptions = new List<Exception>();

        foreach (var file in files)
        {
            try
            {
                file.Delete();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            if (exceptions.Count == 1)
                throw exceptions[0];

            throw new AggregateException(exceptions);
        }
    }

    private async Task<BlobContainerClient> GetBlobContainerClientAsync(string containerName)
    {
        // Create a BlobServiceClient object which will be used to create a container client
        var blobServiceClient = new BlobServiceClient(ConnStr);

        var container = await blobServiceClient.GetBlobContainersAsync()
            .FirstOrDefaultAsync(x => x.Name == containerName);

        return blobServiceClient.GetBlobContainerClient(container.Name);
    }

    private async Task<(FileInfo localFile, CloudBlockBlob sourceBlob, bool shouldBeDownloaded)>
        PrepareBlobDownloadAsync(string containerName, string fileName, FileInfo localFile, bool noDownload)
    {
        CloudBlockBlob sourceBlob = null;
        var shouldBeDownloaded = false;

        try
        {
            var blobContainer = GetCloudBlobContainer(containerName);
            sourceBlob = blobContainer.GetBlockBlobReference(fileName);
            await sourceBlob.FetchAttributesAsync();
            var totalSize = Convert.ToDouble(sourceBlob.Properties.Length);

            var state = GetBlobProgressState(sourceBlob.Name, StorageOperation.Download);
            state.Reset(sourceBlob.Name, totalSize, null);

            if (!noDownload)
            {
                Log.LogInformation($"Preparing download of blob {sourceBlob.Name} to: {localFile.FullName}");
                state.Restart();

                if (!localFile.Exists
                    || !await CheckMD5Async(localFile, sourceBlob))
                    shouldBeDownloaded = true;

                state.Stop();
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex, ex.Message);
        }

        return (localFile, sourceBlob, shouldBeDownloaded);
    }

    private ProgressState GetBlobProgressState(string blobName, StorageOperation operation) =>
        ProgressStates.GetOrAddValue(blobName, () => new(ProgressReporter, operation, null, null));

    private async Task RunBlobDownloadAsync(FileInfo localFile, CloudBlockBlob sourceBlob, bool shouldBeDownloaded)
    {
        var state = GetBlobProgressState(sourceBlob.Name, StorageOperation.Download);

        if (shouldBeDownloaded)
        {
            var totalSize = Convert.ToDouble(sourceBlob.Properties.Length);

            Log.LogInformation(
                $"Downloading blob {sourceBlob.Name} to: {localFile.FullName} {ProgressState.GetProgress(totalSize)}");

            state.Reset(sourceBlob.Name, totalSize, null);

            var context = new SingleTransferContext
            {
                ProgressHandler = new Progress<TransferStatus>(prg => LogProgress(prg, state))
            };

#if NETSTANDARD
            using var downloadFileStream = localFile.OpenWrite();
#else
            await using var downloadFileStream = localFile.OpenWrite();
#endif
            await TransferManager.DownloadAsync(sourceBlob,
                downloadFileStream,
                new()
                {
                    DisableContentMD5Validation = true
                },
                context);
        }

        Log.LogInformation(
            $"Finished download of blob {sourceBlob.Name} in {state.ElapsedTime} to: {localFile.FullName}");
    }

    private void ReportProgress(string prgInfo) => Log.LogInformation(prgInfo);

    private async Task<bool> CheckMD5Async(FileInfo localFile, CloudBlockBlob sourceBlob)
    {
        var state = GetBlobProgressState(sourceBlob.Name, StorageOperation.None);
        bool result;

        if (localFile.Exists)
        {
            var remoteMD5 = sourceBlob.Properties.ContentMD5;
            var log = $"Local file {localFile.FullName} exists, ";

            if (remoteMD5 is null)
            {
                result = false;
                log += $"but it's not possible to compare its checksum - since blob {sourceBlob.Name} hash is missing";
            }
            else
            {
                string localMD5;
                await _md5Semaphore.WaitAsync();

                Log.LogInformation($"Checking MD5 of {localFile.FullName}");

                try
                {
                    localMD5 = localFile.GenerateMd5OfFile();
                }
                finally
                {
                    state.Stop();
                    _md5Semaphore.Release();
                }

                Log.LogInformation($"Generated MD5 of {localFile.FullName} in {state.ElapsedTime}");

                result = localMD5.IsEqual(remoteMD5);

                log += result
                    ? $"and its checksum is equal to blob {sourceBlob.Name} hash"
                    : $"but its checksum differs\n{remoteMD5}\texpected, got:\n{localMD5}";
            }

            Log.LogInformation(log);
        }
        else
        {
            result = false;
        }

        return result;
    }

    private void SaveResults(IDictionary<string, string> resDictionary)
    {
        var resultJson = resDictionary.ToJson(Formatting.Indented);
        File.WriteAllText("result.json", resultJson);
        Log.LogInformation(resultJson);
    }
}