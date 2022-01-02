using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FEx.Extensions;
using FEx.Extensions.Collections;
using FEx.Extensions.Collections.Dictionaries;
using FEx.Extensions.IO;
using FEx.Json;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using Microsoft.Azure.Storage.Shared.Protocol;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;
using DeleteSnapshotsOption = Microsoft.Azure.Storage.Blob.DeleteSnapshotsOption;

namespace FEx.AzureStorage;

public class AzureStorageService : IAzureStorageService
{
    /// <summary>
    ///     The latest version according to
    ///     https://docs.microsoft.com/en-us/rest/api/storageservices/versioning-for-the-azure-storage-services
    /// </summary>
    public const string LatestVersion = "2021-02-12";

    private static string GetBlobName(string path, string fileName)
    {
        return path.IsNotNullOrEmptyString() ? $"{path}/{fileName}" : fileName;
    }

    private static void EnsureDefaultServiceVersion(string connectionString)
    {
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
        ServiceProperties props = blobClient.GetServiceProperties();

        if (props.DefaultServiceVersion == null) //todo - or earlier if configuration allows 
        {
            props.DefaultServiceVersion = LatestVersion;
            blobClient.SetServiceProperties(props);
        }
    }

    private readonly SemaphoreSlim _md5Semaphore;

    public AzureStorageService(ILogger<AzureStorageService> logger)
    {
        ProgressStates = new ConcurrentDictionary<string, ProgressState>();
        Log = logger;
        ProgressReporter = new Progress<string>(ReportProgress);
        _md5Semaphore = new SemaphoreSlim(1, 1);
    }

    protected Progress<string> ProgressReporter { get; }
    protected string ConnStr { get; private set; }
    protected ConcurrentDictionary<string, ProgressState> ProgressStates { get; }
    protected ILogger<AzureStorageService> Log { get; }

    public void Configure(string connStr, int parallelOperationsPerProcessorCount = 8)
    {
        int parallelOperationsCount = Environment.ProcessorCount * parallelOperationsPerProcessorCount;
        ConnStr = connStr;

        ServicePointManager.Expect100Continue = false;
        ServicePointManager.DefaultConnectionLimit = parallelOperationsCount;
        TransferManager.Configurations.ParallelOperations = parallelOperationsCount;
        EnsureDefaultServiceVersion(ConnStr);
    }

    public async Task<(string fileName, FileInfo localPath)> ProcessBlobAsync(string containerName, string downloadDir, string path)
    {
        Log.LogInformation($"Preparing blob for container {containerName} and path {path}");

        BlobContainerClient containerClient = await GetBlobContainerClientAsync(containerName);
        BlobItem blob = await containerClient
            .GetBlobsAsync(prefix: path)
            .OrderByDescending(x => x.Properties.LastModified)
            .FirstOrDefaultAsync();
        string fileName = blob.Name;
        var localFile = new FileInfo(Path.Combine(downloadDir, Path.GetFileName(fileName)));

        return (fileName, localFile);
    }

    public CloudBlobContainer GetCloudBlobContainer(string containerName)
    {
        CloudStorageAccount account = CloudStorageAccount.Parse(ConnStr);
        CloudBlobClient blobClient = account.CreateCloudBlobClient();
        return blobClient.GetContainerReference(containerName);
    }

    public async Task<bool> DownloadLatestBlobsAsync(string downloadDir, string containerName, bool deleteOldFiles = true, string deleteFilesMask = "*.*", bool noDownload = false, params string[] paths)
    {
        Directory.CreateDirectory(downloadDir);

        (string fileName, FileInfo localFile)[] blobsInfo =
            await paths.RunFuncTaskWithWhenAllAsync(path => ProcessBlobAsync(containerName, downloadDir, path));

        if (deleteOldFiles)
        {
            DeleteOldFiles(downloadDir, deleteFilesMask, blobsInfo.Select(x => x.localFile.FullName).ToArray());
        }

        (FileInfo localFile, CloudBlockBlob sourceBlob, bool shouldBeDownloaded)[] preparedBlobs = await blobsInfo.RunFuncTaskWithWhenAllAsync(x =>
            PrepareBlobDownloadAsync(containerName, x.fileName, x.localFile, noDownload), true);

        if (!noDownload)
        {
            await preparedBlobs.RunFuncTaskWithWhenAllAsync(x =>
                RunBlobDownloadAsync(x.localFile, x.sourceBlob, x.shouldBeDownloaded), true);
        }

        IDictionary<string, string> resDictionary = new Dictionary<string, string>();
        IDictionary<string, string> invalidDownloads = new Dictionary<string, string>();

        foreach ((FileInfo localFile, CloudBlockBlob sourceBlob, bool _) in preparedBlobs)
        {
            string path = string.Join("/", Path.GetDirectoryName(sourceBlob.Name).Split('\\'));
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
            string msg = string.Join("\n", invalidDownloads.Select(x => $"{x.Key}: {x.Value}"));
            throw new Exception($"Some blobs weren't successfully downloaded:\n{msg}");
        }

        return true;
    }

    public async Task<IDictionary<FileInfo, CloudBlockBlob>> UploadFilesAsync(string containerName, string path, bool overwrite = false, bool oneByOne = false, params FileInfo[] files)
    {
        if (files.Length == 0)
        {
            throw new Exception("No files to upload");
        }

        CloudBlobContainer container = GetCloudBlobContainer(containerName);

        if (!oneByOne)
        {
            return (await files.RunFuncTaskWithWhenAllAsync(file => UploadFileAsync(path, overwrite, file, containerName, container), true))
                .ToDictionary(x => x.file, x => x.blob);
        }

        var result = new Dictionary<FileInfo, CloudBlockBlob>();

        foreach (FileInfo f in files)
        {
            (FileInfo file, CloudBlockBlob destBlob) = await UploadFileAsync(path, overwrite, f, containerName, container);
            result.Add(file, destBlob);
        }

        return result;
    }

    public async Task<(string file, CloudBlockBlob blob)> UploadStreamAsync(
        string path,
        bool overwrite,
        string fileName,
        Stream stream,
        string containerName = null,
        CloudBlobContainer container = null)
    {
        container = container ?? GetCloudBlobContainer(containerName);
        string blobName = GetBlobName(path, fileName);
        Log.LogInformation($"Preparing blob for container {containerName} and path {blobName}");
        CloudBlockBlob destBlob = container.GetBlockBlobReference(blobName);
        ProgressState state = GetBlobProgressState(blobName, StorageOperation.Upload);
        state.Reset(blobName, Convert.ToDouble(stream.Length));

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

    public async Task<CloudBlockBlobInfo> GetBlobAsync(
        string path,
        CloudBlobContainer container = null,
        string containerName = null,
        CancellationToken cancellationToken = default)
    {
        container = container ?? GetCloudBlobContainer(containerName);
        var destBlob = new CloudBlockBlobInfo(container.GetBlockBlobReference(path));

        if (await destBlob.EnsureExistsAsync(cancellationToken: cancellationToken))
        {
            await destBlob.FetchAttributesAsync(cancellationToken: cancellationToken);
        }

        return destBlob;
    }

    public async Task<IList<CloudBlockBlobInfo>> GetCloudBlockBlobsInfoAsync(string containerName, string path, bool useFlatBlobListing = false)
    {
        return (await GetBlobsAsync<CloudBlockBlob>(containerName, path, useFlatBlobListing)).AsParallel()
            .Select(x => new CloudBlockBlobInfo(x))
            .ToArray();
    }

    public async Task<IList<CloudBlockBlob>> GetCloudBlockBlobsAsync(string containerName, string path, bool useFlatBlobListing = false)
    {
        return await GetBlobsAsync<CloudBlockBlob>(containerName, path, useFlatBlobListing);
    }

    public async Task<IList<T>> GetBlobsAsync<T>(string containerName, string path, bool useFlatBlobListing = false)
        where T : CloudBlob
    {
        CloudBlobContainer container = GetCloudBlobContainer(containerName);
        T[] blobs = container
            .ListBlobs(path, useFlatBlobListing, BlobListingDetails.Metadata)
            .Cast<T>()
            .ToArray();

        await blobs.RunFuncTaskWithWhenAllAsync(x => x.FetchAttributesAsync(), true);
        return blobs;
    }

    public async Task<bool> DeleteBlobAsync(CloudBlockBlob blob,
        DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None,
        AccessCondition accessCondition = null,
        BlobRequestOptions options = null,
        OperationContext operationContext = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default)
        {
            cancellationToken = CancellationToken.None;
        }

        return await blob.DeleteIfExistsAsync(deleteSnapshotsOption, accessCondition, options, operationContext, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string containerName,
        string path,
        string fileName,
        bool primaryOnly = false,
        BlobRequestOptions options = null,
        OperationContext operationContext = null,
        CancellationToken cancellationToken = default)
    {
        CloudBlobContainer container = GetCloudBlobContainer(containerName);
        var blobName = $"{path}/{fileName}";
        CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
        return await ExistsAsync(blob, primaryOnly, options, operationContext, cancellationToken);
    }

    public async Task<bool> ExistsAsync(CloudBlockBlob blob,
        bool primaryOnly = false,
        BlobRequestOptions options = null,
        OperationContext operationContext = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default)
        {
            cancellationToken = CancellationToken.None;
        }

        return await blob.ExistsAsync(primaryOnly, options, operationContext, cancellationToken);
    }

    public async Task<(CloudBlockBlob blob, bool isSuccess)> CopyBlobAsync(string containerName, string srcBlob, string destBlob, bool overwrite = true, Func<CloudBlob, Task> blobAction = null)
    {
        CloudBlobContainer container = GetCloudBlobContainer(containerName);
        CloudBlockBlob sourceBlob = container.GetBlockBlobReference(srcBlob);

        if (await sourceBlob.ExistsAsync())
        {
            CloudBlockBlob destinationBlob = container.GetBlockBlobReference(destBlob);

            ProgressState state = GetBlobProgressState(destBlob, StorageOperation.Upload);
            state.Reset(destBlob, Convert.ToDouble(srcBlob.Length));

            var context = new SingleTransferContext
            {
                ProgressHandler = new Progress<TransferStatus>(prg => LogProgress(prg, state)),
                ShouldOverwriteCallbackAsync = (_, _) => Task.FromResult(overwrite)
            };

            await TransferManager.CopyAsync(sourceBlob, destinationBlob, CopyMethod.ServiceSideAsyncCopy, null, context);

            if (blobAction != null)
            {
                await blobAction(destinationBlob);
            }

            return (destinationBlob, true);
        }

        return (null, false);
    }

    public async Task<(FileInfo file, CloudBlockBlob blob)> UploadFileAsync(
        string path,
        bool overwrite,
        FileInfo file,
        string containerName = null,
        CloudBlobContainer container = null,
        CancellationToken cancellationToken = default)
    {
        container = container ?? GetCloudBlobContainer(containerName);
        string blobName = GetBlobName(path, file.Name);
        Log.LogInformation($"Preparing blob for container {containerName} and path {blobName}");
        CloudBlockBlob destBlob = container.GetBlockBlobReference(blobName);
        ProgressState state = GetBlobProgressState(blobName, StorageOperation.Upload);
        state.Reset(blobName, Convert.ToDouble(file.Length));

        var context = new SingleTransferContext
        {
            ProgressHandler = new Progress<TransferStatus>(prg => LogProgress(prg, state)),
            ShouldOverwriteCallbackAsync = (_, _) => Task.FromResult(overwrite)
        };

        await TransferManager.UploadAsync(file.FullName, destBlob, null, context, cancellationToken);
        await destBlob.FetchAttributesAsync(cancellationToken);
        return (file, destBlob);
    }

    private async Task<BlobContainerClient> GetBlobContainerClientAsync(string containerName)
    {
        // Create a BlobServiceClient object which will be used to create a container client
        var blobServiceClient = new BlobServiceClient(ConnStr);

        BlobContainerItem container = await blobServiceClient.GetBlobContainersAsync().FirstOrDefaultAsync(x => x.Name == containerName);
        return blobServiceClient.GetBlobContainerClient(container.Name);
    }

    private async Task<(FileInfo localFile, CloudBlockBlob sourceBlob, bool shouldBeDownloaded)> PrepareBlobDownloadAsync(string containerName, string fileName, FileInfo localFile, bool noDownload = false)
    {
        CloudBlockBlob sourceBlob = null;
        var shouldBeDownloaded = false;

        try
        {
            CloudBlobContainer blobContainer = GetCloudBlobContainer(containerName);
            sourceBlob = blobContainer.GetBlockBlobReference(fileName);
            await sourceBlob.FetchAttributesAsync();
            var totalSize = Convert.ToDouble(sourceBlob.Properties.Length);

            ProgressState state = GetBlobProgressState(sourceBlob.Name, StorageOperation.Download);
            state.Reset(sourceBlob.Name, totalSize);

            if (!noDownload)
            {
                Log.LogInformation($"Preparing download of blob {sourceBlob.Name} to: {localFile.FullName}");
                state.Restart();

                if (!localFile.Exists || !await CheckMD5Async(localFile, sourceBlob))
                {
                    shouldBeDownloaded = true;
                }

                state.Stop();
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex, ex.Message);
        }

        return (localFile, sourceBlob, shouldBeDownloaded);
    }

    private ProgressState GetBlobProgressState(string blobName, StorageOperation operation)
    {
        return ProgressStates.GetOrAddValue(blobName, () => new ProgressState(ProgressReporter, operation));
    }

    private async Task RunBlobDownloadAsync(FileInfo localFile, CloudBlockBlob sourceBlob, bool shouldBeDownloaded)
    {
        ProgressState state = GetBlobProgressState(sourceBlob.Name, StorageOperation.Download);

        if (shouldBeDownloaded)
        {
            var totalSize = Convert.ToDouble(sourceBlob.Properties.Length);
            Log.LogInformation($"Downloading blob {sourceBlob.Name} to: {localFile.FullName} {ProgressState.GetProgress(totalSize)}");
            state.Reset(sourceBlob.Name, totalSize);

            var context = new SingleTransferContext
            {
                ProgressHandler = new Progress<TransferStatus>(prg => LogProgress(prg, state))
            };

            await using (FileStream downloadFileStream = localFile.OpenWrite())
            {
                await TransferManager.DownloadAsync(sourceBlob, downloadFileStream, new DownloadOptions { DisableContentMD5Validation = true }, context);
            }
        }

        Log.LogInformation($"Finished download of blob {sourceBlob.Name} in {state.ElapsedTime} to: {localFile.FullName}");
    }

    private void ReportProgress(string prgInfo)
    {
        Log.LogInformation(prgInfo);
    }

    private async Task<bool> CheckMD5Async(FileInfo localFile, CloudBlockBlob sourceBlob)
    {
        ProgressState state = GetBlobProgressState(sourceBlob.Name, StorageOperation.None);
        bool result;

        if (localFile.Exists)
        {
            string remoteMD5 = sourceBlob.Properties.ContentMD5;
            var log = $"Local file {localFile.FullName} exists, ";

            if (remoteMD5 == null)
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

                result = localMD5.EqualsIgnoreCase(remoteMD5);

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

    private void LogProgress(TransferStatus progress, ProgressState state)
    {
        state.LogProgress(progress);
    }

    private void SaveResults(IDictionary<string, string> resDictionary)
    {
        string resultJson = resDictionary.ToJson(formatting: Formatting.Indented);
        File.WriteAllText("result.json", resultJson);
        Log.LogInformation(resultJson);
    }

    private void DeleteOldFiles(string downloadDir, string deleteFilesMask, params string[] except)
    {
        FileInfo[] files = new DirectoryInfo(downloadDir)
            .EnumerateFiles(deleteFilesMask)
            .Where(x => except?.Contains(x.FullName) != true)
            .ToArray();

        var exceptions = new List<Exception>();

        foreach (FileInfo file in files)
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
            {
                throw exceptions[0];
            }

            throw new AggregateException(exceptions);
        }
    }
}