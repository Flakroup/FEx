using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.NuGetx.Tests;

public sealed class NuGetManagerTests
{
    private const string PackageId = "Some.Package";

    private readonly FakeResourceSource _source = new();
    private readonly PackageMetadataResource _metadata = Substitute.For<PackageMetadataResource>();
    private readonly DownloadResource _download = Substitute.For<DownloadResource>();

    public NuGetManagerTests()
    {
        _source.Register<PackageMetadataResource>(_metadata);
        _source.Register<DownloadResource>(_download);
    }

    [Fact]
    public async Task Construction_And_Initialization_Make_No_Network_Call()
    {
        using var manager = CreateManager();

        await manager.InitializeAsync();

        _source.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Resources_Are_Created_Once_Under_Concurrency()
    {
        using var manager = CreateManager();
        ReturnVersions();
        ReturnDownload(DownloadResourceResultStatus.Available);

        await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => manager.RestorePackageByIdAsync(PackageId, token: TestContext.Current.CancellationToken))));

        _source.Requests.Count(x => x == typeof(PackageMetadataResource)).ShouldBe(1);
        _source.Requests.Count(x => x == typeof(DownloadResource)).ShouldBe(1);
    }

    [Fact]
    public async Task Failed_Resource_Creation_Is_Not_Cached()
    {
        using var manager = CreateManager();
        ReturnVersions();
        ReturnDownload(DownloadResourceResultStatus.Available);
        _source.FailNextRequest = true;

        await Should.ThrowAsync<System.InvalidOperationException>(() => manager.RestorePackageByIdAsync(PackageId, token: TestContext.Current.CancellationToken));

        var (_, isSuccess) = await manager.RestorePackageByIdAsync(PackageId, token: TestContext.Current.CancellationToken);

        isSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Package_Without_Listed_Versions_Returns_Failure_Instead_Of_Throwing()
    {
        using var manager = CreateManager();
        ReturnMetadata();

        var (result, isSuccess) = await manager.RestorePackageByIdAsync(PackageId, token: TestContext.Current.CancellationToken);

        result.ShouldBeNull();
        isSuccess.ShouldBeFalse();
        await _download.DidNotReceiveWithAnyArgs()
            .GetDownloadResourceResultAsync(default!, default!, default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Only_Unlisted_Versions_Returns_Failure()
    {
        using var manager = CreateManager();
        ReturnMetadata(Metadata("1.0.0", false));

        var (result, isSuccess) = await manager.RestorePackageByIdAsync(PackageId, token: TestContext.Current.CancellationToken);

        result.ShouldBeNull();
        isSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Restores_Highest_Listed_Version_Of_Any_Metadata_Type()
    {
        using var manager = CreateManager();
        ReturnMetadata(Metadata("1.0.0"), Metadata("2.0.0"), Metadata("3.0.0", false));
        ReturnDownload(DownloadResourceResultStatus.Available);

        var (result, isSuccess) = await manager.RestorePackageByIdAsync(PackageId, token: TestContext.Current.CancellationToken);

        isSuccess.ShouldBeTrue();
        result.ShouldNotBeNull();
        await _download.Received(1)
            .GetDownloadResourceResultAsync(Arg.Is<PackageIdentity>(x => x.Version == NuGetVersion.Parse("2.0.0")),
                Arg.Any<PackageDownloadContext>(),
                Arg.Any<string>(),
                Arg.Any<ILogger>(),
                Arg.Any<CancellationToken>());
    }

    // Every test disposes the manager it creates via `using`
#pragma warning disable IDISP004
    private NuGetManager CreateManager() => new(new(NullLogger<NuGetManager>.Instance), _source);
#pragma warning restore IDISP004

    private static IPackageSearchMetadata Metadata(string version, bool isListed = true)
    {
        var metadata = Substitute.For<IPackageSearchMetadata>();
        metadata.Identity.Returns(new PackageIdentity(PackageId, NuGetVersion.Parse(version)));
        metadata.IsListed.Returns(isListed);

        return metadata;
    }

    private void ReturnVersions() => ReturnMetadata(Metadata("1.0.0"));

    private void ReturnMetadata(params IPackageSearchMetadata[] versions) =>
        _metadata.GetMetadataAsync(default!, default, default, default!, default!, default)
            .ReturnsForAnyArgs(Task.FromResult<IEnumerable<IPackageSearchMetadata>>(versions));

#pragma warning disable IDISP004 // The restored result is owned by the code under test
    private void ReturnDownload(DownloadResourceResultStatus status) =>
        _download.GetDownloadResourceResultAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(_ => Task.FromResult(status == DownloadResourceResultStatus.Available
                ? new DownloadResourceResult(new MemoryStream(), "test")
                : new DownloadResourceResult(status)));
#pragma warning restore IDISP004

    private sealed class FakeResourceSource : INuGetResourceSource
    {
        private readonly Dictionary<System.Type, object> _resources = [];
        private readonly List<System.Type> _requests = [];

        public bool FailNextRequest { get; set; }

        public IReadOnlyList<System.Type> Requests
        {
            get
            {
                lock (_requests)
                    return _requests.ToArray();
            }
        }

        public void Register<T>(T resource) where T : class => _resources[typeof(T)] = resource;

        public async Task<T> GetResourceAsync<T>() where T : class, INuGetResource
        {
            lock (_requests)
                _requests.Add(typeof(T));

            await Task.Delay(20); // widen the race window for the concurrency test

            if (FailNextRequest)
            {
                FailNextRequest = false;

                throw new System.InvalidOperationException("network down");
            }

            return (T)_resources[typeof(T)];
        }
    }
}
