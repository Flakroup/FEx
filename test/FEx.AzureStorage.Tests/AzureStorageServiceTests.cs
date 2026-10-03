using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AzureStorage.Tests;

public sealed class AzureStorageServiceTests
{
    private sealed class CountingService : AzureStorageService
    {
        private readonly BlobContainerClient _container;

        public int ResolveCount;

        public CountingService(BlobContainerClient container)
            : base(NullLogger<AzureStorageService>.Instance)
        {
            _container = container;
        }

        protected override Task<(FileInfo localFile, CloudBlockBlob? sourceBlob, bool shouldBeDownloaded)>
            PrepareBlobDownloadAsync(string containerName, string fileName, FileInfo localFile, bool noDownload) =>
            Task.FromResult<(FileInfo, CloudBlockBlob?, bool)>(
                (localFile, new CloudBlockBlob(new Uri("http://blob.invalid/c/" + fileName)), false));

        protected override Task<BlobContainerClient> GetBlobContainerClientAsync(string containerName)
        {
            Interlocked.Increment(ref ResolveCount);

            return Task.FromResult(_container);
        }
    }

    private static BlobContainerClient CreateContainer(bool nested = true)
    {
        var container = Substitute.For<BlobContainerClient>();
        container.GetBlobsAsync(Arg.Any<BlobTraits>(), Arg.Any<BlobStates>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var prefix = call.ArgAt<string>(2);
                var item = BlobsModelFactory.BlobItem((nested ? prefix + "/" : string.Empty) + prefix + ".txt",
                    properties: BlobsModelFactory.BlobItemProperties(true, lastModified: DateTimeOffset.UtcNow));

                return AsyncPageable<BlobItem>.FromPages([Page<BlobItem>.FromValues([item], null, Substitute.For<Response>())]);
            });

        return container;
    }

    [Fact]
    public async Task ProcessBlobsAsync_ManyPaths_ResolvesContainerOnce()
    {
        var service = new CountingService(CreateContainer(false));

        var res = await service.ProcessBlobsAsync("c", Path.GetTempPath(), ["a", "b", "c", "d"]);

        res.Select(x => x.fileName).ShouldBe(["a.txt", "b.txt", "c.txt", "d.txt"]);
        service.ResolveCount.ShouldBe(1);
    }

    [Fact]
    public async Task DownloadLatestBlobsAsync_ManyPaths_ResolvesContainerOnce()
    {
        var service = new CountingService(CreateContainer());
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            foreach (var name in new[] { "a", "b", "c" })
                await File.WriteAllTextAsync(Path.Combine(dir, name + ".txt"), name, TestContext.Current.CancellationToken);

            (await service.DownloadLatestBlobsAsync(dir, "c", false, "*", true, "a", "b", "c")).ShouldBeTrue();
            service.ResolveCount.ShouldBe(1);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
