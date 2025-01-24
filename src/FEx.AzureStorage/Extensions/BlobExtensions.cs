using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FEx.Abstractions.Flow;
using FEx.Abstractions.Flow.Errors;
using FEx.Common.Extensions;
using FEx.Extensions;
using FEx.Extensions.Base.Helpers;
using FEx.Extensions.Base.IO;
using FEx.Extensions.Collections.Dictionaries;
using FEx.Webx;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.AzureStorage.Extensions;

public static class BlobExtensions
{
    public static string GetBlobChecksum(this CloudBlockBlob blob) =>
        blob?.Properties?.ContentMD5 is not null
            ? Convert.FromBase64String(blob.Properties.ContentMD5).GetHashString()
            : null;

    public static Uri GetBlobUri(this CloudBlockBlob blob) => blob?.Uri?.AbsoluteUri.ToUri();

    public static string GetBlobChecksum(this BlobItem blob) => blob?.Properties?.ContentHash?.GetHashString();

    public static Uri GetBlobUri(this BlobItem blob, BlobContainerClient blobContainerClient)
    {
        BlobClient blobClient = blobContainerClient.GetBlobClient(blob.Name);
        Uri blobUri = blobClient.Uri;

        return new($"{blobUri.Scheme}://{blobUri.Host}{blobUri.LocalPath}");
    }

    public static async Task<IList<IListBlobItem>> ListBlobsAsync(this CloudBlobDirectory directory,
                                                                  CancellationToken cancellationToken)
    {
        BlobContinuationToken continuationToken = null;
        var results = new List<IListBlobItem>();

        do
        {
            BlobResultSegment response = await directory.ListBlobsSegmentedAsync(continuationToken, cancellationToken);
            continuationToken = response.ContinuationToken;
            results.AddRange(response.Results);
        } while (continuationToken is not null);

        return results;
    }

    public static async Task<IList<CloudBlobContainer>> ListContainersAsync(
        this CloudBlobClient client,
        CancellationToken cancellationToken)
    {
        BlobContinuationToken continuationToken = null;
        var results = new List<CloudBlobContainer>();

        do
        {
            ContainerResultSegment response =
                await client.ListContainersSegmentedAsync(continuationToken, cancellationToken);

            continuationToken = response.ContinuationToken;
            results.AddRange(response.Results);
        } while (continuationToken is not null);

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
        if (prefix.IsNotNullOrEmptyString())
        {
            string dir = FileSystemHelper.GetParentFolderFromPath(prefix, '/', true);
            CloudBlobDirectory directory = client.GetDirectoryReference(dir);
            IList<IListBlobItem> blobs = await directory.ListBlobsAsync(cancellationToken);

            if (blobs.OfType<CloudBlobDirectory>().All(x => x.Prefix != prefix))
                return Result<IList<IListBlobItem>, StackError>.Failure;
        }

        BlobContinuationToken continuationToken = null;
        var results = new List<IListBlobItem>();

        do
        {
            BlobResultSegment response = await client.ListBlobsSegmentedAsync(prefix,
                useFlatBlobListing,
                blobListingDetails,
                0,
                continuationToken,
                options,
                operationContext,
                cancellationToken);

            continuationToken = response.ContinuationToken;
            results.AddRange(response.Results);
        } while (continuationToken is not null);

        return results;
    }

    public static async Task EnsureCorrectContentTypeAsync(this CloudBlockBlobInfo arg)
    {
        string contentType = MimeTypesUtility.Mappings.TryGetReadOnlyKeyValue(arg.Extension.TrimStart('.'));

        if (contentType != null
            && arg.ContentType != contentType)
        {
            arg.ContentType = contentType;
            await arg.Blob.SetPropertiesAsync();
        }
    }
}