using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Abstractions;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
#if NETSTANDARD2_0
using NuGet.Protocol;
#endif
using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using INuGetLogger = NuGet.Common.ILogger;
using NuGetRepository = NuGet.Protocol.Core.Types.Repository;
using NuGetSettings = NuGet.Configuration.Settings;

namespace FEx.NuGetx;

public class NuGetManager : AsyncInitializable
{
    private readonly SemaphoreSlim _sem;

    private int _counter;

    private NuGetLogger<NuGetManager> Logger { get; }
    private List<Lazy<INuGetResourceProvider>> Providers { get; }
    private AsyncOnce<PackageMetadataResource> PackageMetadataResource { get; }
    private AsyncOnce<PackageUpdateResource> PackageUpdateResource { get; }
    private AsyncOnce<DownloadResource> DownloadResource { get; }
    private SourceCacheContext SourceCacheContext { get; }

    private ISettings Settings { get; }

    public NuGetManager(NuGetLogger<NuGetManager> logger) : this(logger, new NuGetOrgResourceSource())
    {
    }

    internal NuGetManager(NuGetLogger<NuGetManager> logger, INuGetResourceSource resourceSource)
    {
        Logger = logger;
        PackageMetadataResource = new(resourceSource.GetResourceAsync<PackageMetadataResource>);
        PackageUpdateResource = new(resourceSource.GetResourceAsync<PackageUpdateResource>);
        DownloadResource = new(resourceSource.GetResourceAsync<DownloadResource>);
        Providers = [];
        SourceCacheContext = new();
        Settings = NuGetSettings.LoadDefaultSettings(null);
        _sem = new(4, 4);

        BeginInitialization();
    }

    public static async Task<PackageMetadataResource> GetNuGetOrgPackageMetadataResourceAsync() =>
        await new NuGetOrgResourceSource().GetResourceAsync<PackageMetadataResource>(CancellationToken.None);

    public static async Task<PackageIdentity[]> GetIdentitiesAsync(
        PackageSearchMetadataBuilder.ClonedPackageSearchMetadata package) =>
        (await package.GetVersionsAsync()).Select(x => new PackageIdentity(package.Identity.Id, x.Version)).ToArray();

    public static async Task GetPackageDependenciesAsync(PackageIdentity package,
                                                         NuGetFramework framework,
                                                         SourceCacheContext cacheContext,
                                                         INuGetLogger logger,
                                                         IEnumerable<SourceRepository> repositories,
                                                         ISet<SourcePackageDependencyInfo> availablePackages)
    {
        if (!availablePackages.Contains(package))
            foreach (var sourceRepository in repositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();

                var dependencyInfo = await dependencyInfoResource.ResolvePackage(package,
                    framework,
                    cacheContext,
                    logger,
                    CancellationToken.None);

                if (dependencyInfo is not null)
                {
                    availablePackages.Add(dependencyInfo);

                    foreach (var dependency in dependencyInfo.Dependencies)
                    {
                        await GetPackageDependenciesAsync(new(dependency.Id, dependency.VersionRange.MinVersion),
                            framework,
                            cacheContext,
                            logger,
                            repositories,
                            availablePackages);
                    }
                }
            }
    }

    public async Task BackupPackageAsync(PackageIdentity package,
                                         DirectoryInfo backupDirectory,
                                         int allPackagesCount,
                                         ISettings settings,
                                         DownloadResource downloadResource,
                                         SourceCacheContext cacheContext,
                                         CancellationToken token)
    {
        await _sem.WaitAsync(token);

        try
        {
            var succeeded = false;
            var retry = true;

            while (!succeeded && retry)
            {
                var (downloadResult, isSuccess) =
                    await RestorePackageAsync(package, downloadResource, cacheContext, settings);

                if (isSuccess)
                {
                    // isSuccess == true guarantees a non-null download result (see RestorePackageAsync).
                    using (var targetPackageStream = (FileStream)downloadResult!.PackageStream)
                    {
                        var bqFile = new FileInfo(Path.Combine(backupDirectory.FullName,
                            Path.GetFileName(targetPackageStream.Name)));

                        bool backup;

                        if (bqFile.Exists)
                        {
                            string? targetContentHash = null;
                            string? bqContentHash = null;

                            try
                            {
                                using (var bqPackageStream = bqFile.OpenRead())
                                {
                                    using var packageArchiveReader = new PackageArchiveReader(bqPackageStream);
                                    bqContentHash = packageArchiveReader.GetContentHash(token);
                                }

                                using (var packageArchiveReader = new PackageArchiveReader(targetPackageStream, true))
                                    targetContentHash = packageArchiveReader.GetContentHash(token);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex.ToString());
                            }

                            backup = bqContentHash != targetContentHash;
                        }
                        else
                        {
                            backup = true;
                        }

                        if (backup)
                        {
                            using var bqPackageStream = bqFile.Open(FileMode.OpenOrCreate,
                                FileAccess.ReadWrite,
                                FileShare.None);

                            bqPackageStream.SetLength(0);
                            bqPackageStream.Seek(0, SeekOrigin.Begin);
                            await targetPackageStream.CopyToAsync(bqPackageStream, token);
                        }
                    }

                    succeeded = true;
                }
                else
                {
                    // A failed-but-non-null result carries a Status; a null result here preserves the
                    // pre-nullable behaviour of throwing (caught by the outer try) rather than looping.
                    retry = downloadResult!.Status != DownloadResourceResultStatus.NotFound;
                }
            }

            Logger.LogInformation(
                $"{Interlocked.Increment(ref _counter)}\\{allPackagesCount} {package.Id} {package.Version} {succeeded}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.ToString());
        }
        finally
        {
            _sem.Release();
        }
    }

    public async Task<(DownloadResourceResult? result, bool isSuccess)> RestorePackageByIdAsync(
        string packageId,
        bool includePrerelease = false,
        bool includeUnlisted = false,
        CancellationToken token = default) =>
        await RestorePackageByIdAsync(packageId,
            await PackageMetadataResource.GetAsync(token),
            SourceCacheContext,
            await DownloadResource.GetAsync(token),
            includePrerelease,
            includeUnlisted,
            token);

    public async Task UnlistAllPackageVersionsAsync(string packageId,
                                                    PackageMetadataResource packageMetadataResource,
                                                    SourceCacheContext sourceCacheContext,
                                                    string apiKey)
    {
        var listedPackages =
            (await packageMetadataResource.GetMetadataAsync(packageId,
                true,
                true,
                sourceCacheContext,
                Logger,
                CancellationToken.None)).Where(x => x.IsListed)
            .ToArray();

        foreach (var pkg in listedPackages)
            await DeletePackageAsync(pkg, apiKey);
    }

    public async Task<bool> DeletePackageAsync(IPackageSearchMetadata pkgToDel, string apiKey)
    {
        if (pkgToDel is not null)
            try
            {
                var packageUpdateResource = await PackageUpdateResource.GetAsync();

                await DeletePackageVersionAsync(packageUpdateResource,
                    pkgToDel.Identity.Id,
                    pkgToDel.Identity.Version.OriginalVersion,
                    apiKey);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }

        return false;
    }

    // Seam over the non-virtual PackageUpdateResource.Delete so the id/version it is called with can be asserted.
    internal virtual Task DeletePackageVersionAsync(PackageUpdateResource resource,
                                                    string packageId,
                                                    string? version,
                                                    string apiKey) =>
        resource.Delete(packageId, version, _ => apiKey, _ => true, false, Logger);

    public async Task<(DownloadResourceResult? result, bool isSuccess)> RestorePackageAsync(
        PackageIdentity pkgToRestore,
        DownloadResource downloadResource,
        SourceCacheContext sourceCacheContext,
        ISettings settings,
        bool directDownload = false,
        string? directDownloadDirectory = null)
    {
        if (pkgToRestore is not null)
            try
            {
                var res = await downloadResource.GetDownloadResourceResultAsync(pkgToRestore,
                    directDownload
                        ? new(sourceCacheContext, directDownloadDirectory, true)
                        : new PackageDownloadContext(sourceCacheContext),
                    SettingsUtility.GetGlobalPackagesFolder(settings),
                    Logger,
                    CancellationToken.None);

                return (res,
                    res.Status != DownloadResourceResultStatus.Cancelled
                    && res.Status != DownloadResourceResultStatus.NotFound);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }

        return (null, false);
    }

    public async Task<FileInfo[]> GetNuGetsToPublishOnNuGetOrgAsync(FileInfo[] allNuGets,
                                                                    HashSet<string>? excludedPackageNames = null,
                                                                    params string[] packagesToPublish)
    {
        var packageMetadataResource = await PackageMetadataResource.GetAsync();

        return await GetNuGetsToPublishAsync(packageMetadataResource,
            allNuGets,
            excludedPackageNames,
            packagesToPublish);
    }

    public async Task<FileInfo[]> GetNuGetsToPublishAsync(PackageMetadataResource packageMetadataResource,
                                                          FileInfo[] allNuGets,
                                                          HashSet<string>? excludedPackageNames = null,
                                                          params string[] packagesToPublish)
    {
        using var sourceCacheContext = new SourceCacheContext();

        var result = await allNuGets.WithWhenAllTasksAsync(x => NuGetNotPublishedAsync(packageMetadataResource,
            sourceCacheContext,
            x,
            excludedPackageNames,
            true,
            true,
            packagesToPublish));

        return result.Where(x => x.isNotPublished).Select(x => x.nuGet).ToArray();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        Providers.AddRange(NuGetRepository.Provider.GetCoreV3());
    }

    private async Task<(DownloadResourceResult? result, bool isSuccess)> RestorePackageByIdAsync(
        string packageId,
        PackageMetadataResource packageMetadataResource,
        SourceCacheContext sourceCacheContext,
        DownloadResource downloadResource,
        bool includePrerelease = false,
        bool includeUnlisted = false,
        CancellationToken token = default)
    {
        var listedPackages =
            (await packageMetadataResource.GetMetadataAsync(packageId,
                includePrerelease,
                includeUnlisted,
                sourceCacheContext,
                Logger,
                token)).Where(x => x.IsListed)
            .ToArray();

        if (listedPackages.Length == 0)
        {
            Logger.LogWarning(
                $"No listed versions of package '{packageId}' were found (includePrerelease: {includePrerelease}, includeUnlisted: {includeUnlisted}); nothing to restore");

            return (null, false);
        }

        var latest = listedPackages.OrderByDescending(x => x.Identity.Version).First();

        return await RestorePackageAsync(latest.Identity, downloadResource, sourceCacheContext, Settings);
    }

    private async Task<(FileInfo nuGet, bool isNotPublished)> NuGetNotPublishedAsync(
        PackageMetadataResource packageMetadataResource,
        SourceCacheContext sourceCacheContext,
        FileInfo file,
        HashSet<string>? excludedPackageNames = null,
        bool includePrerelease = false,
        bool includeUnlisted = false,
        ICollection<string>? filterIds = null,
        CancellationToken token = default)
    {
        PackageIdentity identity;

        using (var pkgStream = file.OpenRead())
        {
            using var packageArchiveReader = new PackageArchiveReader(pkgStream);
            identity = packageArchiveReader.NuspecReader.GetIdentity();
        }

        if (excludedPackageNames is not null
            && excludedPackageNames.IsNotNullOrEmptyCollection()
            && excludedPackageNames.Contains(identity.Id))
            return (file, false);

        IPackageSearchMetadata[]? listedPackages = null;

        if (filterIds is null
            || filterIds.IsNullOrEmptyCollection()
            || filterIds.Contains(identity.Id))
            listedPackages =
                (await packageMetadataResource.GetMetadataAsync(identity.Id,
                    includePrerelease,
                    includeUnlisted,
                    sourceCacheContext,
                    Logger,
                    token)).ToArray();

        return (file, listedPackages?.All(x => !x.Identity.Version.Equals(identity.Version)) ?? false);
    }
}