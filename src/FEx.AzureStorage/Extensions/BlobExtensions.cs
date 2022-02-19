using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FEx.Extensions;
using FEx.Extensions.Helpers;
using FEx.Utilities.Flow;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace FEx.AzureStorage.Extensions;

public static class BlobExtensions
{
    public static string GetBlobChecksum(this CloudBlockBlob blob)
    {
        return blob?.Properties?.ContentMD5 != null
            ? Convert.FromBase64String(blob.Properties.ContentMD5).GetHashString()
            : null;
    }

    public static Uri GetBlobUri(this CloudBlockBlob blob)
    {
        return blob?.Uri?.AbsoluteUri?.ToUri();
    }

    public static string GetBlobChecksum(this BlobItem blob)
    {
        return blob?.Properties?.ContentHash?.GetHashString();
    }

    public static Uri GetBlobUri(this BlobItem blob, BlobContainerClient blobContainerClient)
    {
        BlobClient blobClient = blobContainerClient.GetBlobClient(blob.Name);
        Uri blobUri = blobClient.Uri;
        return new Uri($"{blobUri.Scheme}://{blobUri.Host}{blobUri.LocalPath}");
    }

    public static async Task<IList<IListBlobItem>> ListBlobsAsync(this CloudBlobDirectory directory, CancellationToken cancellationToken)
    {
        BlobContinuationToken continuationToken = null;
        var results = new List<IListBlobItem>();
        do
        {
            BlobResultSegment response = await directory.ListBlobsSegmentedAsync(continuationToken, cancellationToken);
            continuationToken = response.ContinuationToken;
            results.AddRange(response.Results);
        } while (continuationToken != null);

        return results;
    }

    public static async Task<IList<CloudBlobContainer>> ListContainersAsync(this CloudBlobClient client, CancellationToken cancellationToken)
    {
        BlobContinuationToken continuationToken = null;
        var results = new List<CloudBlobContainer>();
        do
        {
            ContainerResultSegment response = await client.ListContainersSegmentedAsync(continuationToken, cancellationToken);
            continuationToken = response.ContinuationToken;
            results.AddRange(response.Results);
        } while (continuationToken != null);

        return results;
    }

    public static async Task<Result<IList<IListBlobItem>, StackError>> ListBlobsAsync(
        this CloudBlobContainer client,
        string prefix,
        CancellationToken cancellationToken,
        bool useFlatBlobListing = false,
        BlobListingDetails blobListingDetails = BlobListingDetails.None,
        BlobRequestOptions options = null,
        OperationContext operationContext = null)
    {
        CloudBlobDirectory directory = client.GetDirectoryReference(prefix);

        if (!(await directory.ListBlobsAsync(cancellationToken)).Any())
        {
            return Result<IList<IListBlobItem>, StackError>.Failure;
        }

        BlobContinuationToken continuationToken = null;
        var results = new List<IListBlobItem>();
        do
        {
            BlobResultSegment response = await client.ListBlobsSegmentedAsync(prefix, useFlatBlobListing, blobListingDetails, new int?(), continuationToken, options, operationContext, cancellationToken);
            continuationToken = response.ContinuationToken;
            results.AddRange(response.Results);
        } while (continuationToken != null);

        return results;
    }
}