using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FEx.AzureStorage;
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

        protected override Task<BlobContainerClient> GetBlobContainerClientAsync(string containerName)
        {
            Interlocked.Increment(ref ResolveCount);

            return Task.FromResult(_container);
        }
    }

    [Fact]
    public async Task ProcessBlobsAsync_ManyPaths_ResolvesContainerOnce()
    {
        var container = Substitute.For<BlobContainerClient>();
        container.GetBlobsAsync(Arg.Any<BlobTraits>(), Arg.Any<BlobStates>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var prefix = call.ArgAt<string>(2);
                var item = BlobsModelFactory.BlobItem(prefix + ".txt",
                    properties: BlobsModelFactory.BlobItemProperties(true, lastModified: DateTimeOffset.UtcNow));

                return AsyncPageable<BlobItem>.FromPages([Page<BlobItem>.FromValues([item], null, Substitute.For<Response>())]);
            });
        var service = new CountingService(container);
        var dir = Path.GetTempPath();

        var res = await service.ProcessBlobsAsync("c", dir, ["a", "b", "c", "d"]);

        res.Select(x => x.fileName).ShouldBe(["a.txt", "b.txt", "c.txt", "d.txt"]);
        service.ResolveCount.ShouldBe(1);
    }
}
