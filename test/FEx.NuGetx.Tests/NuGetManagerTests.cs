using NSubstitute;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using EventId = Microsoft.Extensions.Logging.EventId;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using ManagerLogger = Microsoft.Extensions.Logging.ILogger<FEx.NuGetx.NuGetManager>;

namespace FEx.NuGetx.Tests;

public sealed class NuGetManagerTests
{
    private const string PackageId = "Some.Package";

    private readonly FakeResourceSource _source = new();
    private readonly ListLogger _log = new();
    private readonly PackageMetadataResource _metadata = Substitute.For<PackageMetadataResource>();
    private readonly DownloadResource _download = Substitute.For<DownloadResource>();
    private readonly FakeUpdateResource _update = new();

    public NuGetManagerTests()
    {
        _source.Register(_metadata);
        _source.Register(_download);
        _source.Register<PackageUpdateResource>(_update);
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

        await Should.ThrowAsync<InvalidOperationException>(() => manager.RestorePackageByIdAsync(PackageId, token: TestContext.Current.CancellationToken));

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
        _log.Entries.ShouldContain(x => x.Level == LogLevel.Warning && x.Message.Contains(PackageId));
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

    [Fact(Timeout = 10000)]
    public async Task Cancelled_Caller_Returns_Promptly_And_Does_Not_Poison_Others()
    {
        using var manager = CreateManager();
        ReturnVersions();
        ReturnDownload(DownloadResourceResultStatus.Available);
        _source.HangFor = Timeout.InfiniteTimeSpan;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Should.ThrowAsync<OperationCanceledException>(() =>
            manager.RestorePackageByIdAsync(PackageId, token: cts.Token));

        _source.HangFor = null;
        var (_, isSuccess) = await manager.RestorePackageByIdAsync(PackageId, token: TestContext.Current.CancellationToken);

        isSuccess.ShouldBeTrue();
    }

    [Fact(Timeout = 10000)]
    public async Task Waiter_Queued_On_A_Hanging_Creation_Can_Be_Cancelled()
    {
        using var manager = CreateManager();
        ReturnVersions();
        _source.HangFor = Timeout.InfiniteTimeSpan;
        using var leaderCts = new CancellationTokenSource();
        var leader = Task.Run(() => manager.RestorePackageByIdAsync(PackageId, token: leaderCts.Token),
            TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken); // let the leader take the creation lock
        using var waiterCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Should.ThrowAsync<OperationCanceledException>(() =>
            manager.RestorePackageByIdAsync(PackageId, token: waiterCts.Token));

        await leaderCts.CancelAsync();
#pragma warning disable VSTHRD003 // The leader was started by this test
        await Should.ThrowAsync<OperationCanceledException>(() => leader);
#pragma warning restore VSTHRD003
    }

    [Fact]
    public async Task UnlistAll_Deletes_Each_Listed_Version_By_Id_And_Version()
    {
        using var manager = CreateManager();
        ReturnMetadata(Metadata("1.0.0"), Metadata("2.0.0-beta"), Metadata("3.0.0", false));

        using var cache = new SourceCacheContext();

        await manager.UnlistAllPackageVersionsAsync(PackageId, _metadata, cache, "key");

        manager.Deleted.ShouldBe([(PackageId, "1.0.0", "key"), (PackageId, "2.0.0-beta", "key")]);
        _source.Requests.ShouldContain(typeof(PackageUpdateResource));
    }

    [Fact]
    public async Task DeletePackage_Reports_Failure_When_Delete_Throws()
    {
        using var manager = CreateManager();
        manager.Throw = true;

        (await manager.DeletePackageAsync(Metadata("1.0.0"), "key")).ShouldBeFalse();
        (await manager.DeletePackageAsync(null!, "key")).ShouldBeFalse();
    }

    [Fact]
    public async Task GetNuGetsToPublish_Returns_Only_Packages_Whose_Version_Is_Not_On_The_Feed()
    {
        using var manager = CreateManager();
        using var dir = new TempDirectory();
        var published = dir.CreatePackage(PackageId, "1.0.0");
        var fresh = dir.CreatePackage(PackageId, "2.0.0");
        ReturnMetadata(Metadata("1.0.0"));

        var result = await manager.GetNuGetsToPublishAsync(_metadata, [published, fresh]);

        result.Select(x => x.Name).ShouldBe([fresh.Name]);
    }

    [Fact]
    public async Task GetNuGetsToPublishOnNuGetOrg_Uses_The_Lazy_Resource_Source()
    {
        using var manager = CreateManager();
        using var dir = new TempDirectory();
        var fresh = dir.CreatePackage(PackageId, "2.0.0");
        ReturnMetadata(Metadata("1.0.0"));

        var result = await manager.GetNuGetsToPublishOnNuGetOrgAsync([fresh]);

        result.Length.ShouldBe(1);
        _source.Requests.Count(x => x == typeof(PackageMetadataResource)).ShouldBe(1);
    }

    // Every test disposes the manager it creates via `using`
#pragma warning disable IDISP004
    private TestableManager CreateManager() => new(new(_log), _source);
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

    private sealed class FakeUpdateResource() : PackageUpdateResource(null!, null!);

    private sealed class TestableManager(NuGetLogger<NuGetManager> logger, INuGetResourceSource source)
        : NuGetManager(logger, source)
    {
        public List<(string Id, string? Version, string ApiKey)> Deleted { get; } = [];

        public bool Throw { get; set; }

        internal override Task DeletePackageVersionAsync(PackageUpdateResource resource,
                                                         string packageId,
                                                         string? version,
                                                         string apiKey)
        {
            if (Throw)
                throw new InvalidOperationException("delete failed");

            Deleted.Add((packageId, version, apiKey));

            return Task.CompletedTask;
        }
    }

    private sealed class ListLogger : ManagerLogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    private sealed class TempDirectory : IDisposable
    {
        private readonly string _path = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;

        public FileInfo CreatePackage(string id, string version)
        {
            var file = new FileInfo(Path.Combine(_path, $"{id}.{version}.nupkg"));

            using var stream = file.Create();
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
            using var writer = new StreamWriter(zip.CreateEntry($"{id}.nuspec").Open());

            writer.Write(
                $"<?xml version=\"1.0\"?><package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\"><metadata><id>{id}</id><version>{version}</version><authors>a</authors><description>d</description></metadata></package>");

            return file;
        }

        public void Dispose() => Directory.Delete(_path, true);
    }

    private sealed class FakeResourceSource : INuGetResourceSource
    {
        private readonly Dictionary<Type, object> _resources = [];
        private readonly List<Type> _requests = [];

        public bool FailNextRequest { get; set; }

        public TimeSpan? HangFor { get; set; }

        public IReadOnlyList<Type> Requests
        {
            get
            {
                lock (_requests)
                    return _requests.ToArray();
            }
        }

        public void Register<T>(T resource) where T : class => _resources[typeof(T)] = resource;

        public async Task<T> GetResourceAsync<T>(CancellationToken token) where T : class, INuGetResource
        {
            lock (_requests)
                _requests.Add(typeof(T));

            await Task.Delay(HangFor ?? TimeSpan.FromMilliseconds(20), token); // widens the race window

            if (FailNextRequest)
            {
                FailNextRequest = false;

                throw new InvalidOperationException("network down");
            }

            return (T)_resources[typeof(T)];
        }
    }
}
