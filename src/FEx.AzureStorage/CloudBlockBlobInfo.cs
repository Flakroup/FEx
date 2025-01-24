using FEx.AzureStorage.Extensions;
using FEx.Common.Extensions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.AzureStorage;

public class CloudBlockBlobInfo
{
    public CloudBlockBlob Blob { get; }
    public Uri Uri { get; protected set; }
    public string Checksum { get; protected set; }
    public string Name { get; protected set; }
    public string Extension { get; protected set; }
    public bool Exists { get; protected set; }

    public IDictionary<string, string> Metadata => Blob.Metadata;
    public long Size => Blob.Properties.Length;

    public string ContentType
    {
        get => Blob.Properties.ContentType;
        set => Blob.Properties.ContentType = value;
    }

    public CloudBlockBlobInfo(CloudBlockBlob blob, bool? exists = null)
    {
        Blob = blob;
        EnsureMetadata();

        if (exists.HasValue)
            Exists = exists.Value;
    }

    public static implicit operator CloudBlockBlobInfo(CloudBlockBlob blob) => new(blob);

    public string GetMetadata(string key) => Metadata.TryGetKeyValue(key);

    public async Task<bool> EnsureExistsAsync(bool primaryOnly = false,
                                              BlobRequestOptions options = null,
                                              OperationContext operationContext = null,
                                              CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None)
            cancellationToken = CancellationToken.None;

        Exists = await Blob.ExistsAsync(primaryOnly, options, operationContext, cancellationToken);

        return Exists;
    }

    /// <summary>
    ///     Fetches the attributes asynchronous.
    /// </summary>
    /// <param name="accessCondition">The access condition.</param>
    /// <param name="options">The options.</param>
    /// <param name="operationContext">The operation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task FetchAttributesAsync(AccessCondition accessCondition = null,
                                           BlobRequestOptions options = null,
                                           OperationContext operationContext = null,
                                           CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None)
            cancellationToken = CancellationToken.None;

        if (await EnsureExistsAsync(cancellationToken: cancellationToken))
        {
            await Blob.FetchAttributesAsync(accessCondition, options, operationContext, cancellationToken);
            EnsureMetadata();
        }
    }

    protected void EnsureMetadata()
    {
        Uri = Blob.GetBlobUri();
        Checksum = Blob.GetBlobChecksum();
        Name = Blob.Name;
        Extension = Path.GetExtension(Blob.Name);
    }
}