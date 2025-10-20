using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Abstractions;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
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
    private SourceRepository SourceRepository { get; set; }
    private PackageMetadataResource PackageMetadataResource { get; set; }
    private PackageUpdateResource PackageUpdateResource { get; set; }
    private DownloadResource DownloadResource { get; set; }
    private SourceCacheContext SourceCacheContext { get; }

    private ISettings Settings { get; }

    public NuGetManager(NuGetLogger<NuGetManager> logger)
    {
        Logger = logger;
        Providers = [];
        SourceCacheContext = new();
        Settings = NuGetSettings.LoadDefaultSettings(null);
        _sem = new(4, 4);

        BeginInitialization();
    }

    public static async Task<PackageMetadataResource> GetNuGetOrgPackageMetadataResourceAsync()
    {
        SourceRepository sourceRepository = NuGetRepository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

        return await sourceRepository.GetResourceAsync<PackageMetadataResource>();
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
                (DownloadResourceResult downloadResult, bool isSuccess) =
                    await RestorePackageAsync(package, downloadResource, cacheContext, settings);

                if (isSuccess)
                {
                    using (var targetPackageStream = (FileStream)downloadResult.PackageStream)
                    {
                        var bqFile = new FileInfo(Path.Combine(backupDirectory.FullName,
                            Path.GetFileName(targetPackageStream.Name)));

                        bool backup;

                        if (bqFile.Exists)
                        {
                            string targetContentHash = null;
                            string bqContentHash = null;

                            try
                            {
                                using (FileStream bqPackageStream = bqFile.OpenRead())
                                {
                                    using var packageArchiveReader = new PackageArchiveReader(bqPackageStream);
                                    bqContentHash = packageArchiveReader.GetContentHash(token);
                                }

                                using (var packageArchiveReader = new PackageArchiveReader(targetPackageStream, true))
                                    targetContentHash = packageArchiveReader.GetContentHash(token);
                            }
                            catch //(Exception ex)
                            {
                                // Logger.LogError(ex.ToString());
                            }

                            backup = bqContentHash != targetContentHash;
                        }
                        else
                        {
                            backup = true;
                        }

                        if (backup)
                        {
                            using FileStream bqPackageStream = bqFile.Open(FileMode.OpenOrCreate,
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
                    retry = downloadResult.Status != DownloadResourceResultStatus.NotFound;
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

    public static async Task<PackageIdentity[]> GetIdentitiesAsync(
        PackageSearchMetadataBuilder.ClonedPackageSearchMetadata package) => (await package.GetVersionsAsync()).Select(x => new PackageIdentity(package.Identity.Id, x.Version))
            .ToArray();

    public async Task<(DownloadResourceResult result, bool isSuccess)> RestorePackageByIdAsync(
        string packageId,
        bool includePrerelease = false,
        bool includeUnlisted = false,
        CancellationToken token = default) =>
        await RestorePackageByIdAsync(packageId,
            PackageMetadataResource,
            SourceCacheContext,
            DownloadResource,
            includePrerelease,
            includeUnlisted,
            token);

    public async Task UnlistAllPackageVersionsAsync(string packageId,
                                                    PackageMetadataResource packageMetadataResource,
                                                    SourceCacheContext sourceCacheContext,
                                                    string apiKey)
    {
        PackageSearchMetadataRegistration[] listedPackages =
            (await packageMetadataResource.GetMetadataAsync(packageId,
                true,
                true,
                sourceCacheContext,
                Logger,
                CancellationToken.None)).Where(x => x.IsListed)
            .Cast<PackageSearchMetadataRegistration>()
            .ToArray();

        //await listedPackages.WithWhenAllAsync(x => DeletePackage(x, source, ApiKey));
        foreach (PackageSearchMetadataRegistration pkg in listedPackages)
            await DeletePackageAsync(pkg, apiKey);
    }

    public async Task<bool> DeletePackageAsync(PackageSearchMetadataRegistration pkgToDel, string apiKey)
    {
        if (pkgToDel is not null)
            try
            {
                await PackageUpdateResource.Delete(pkgToDel.PackageId,
                    pkgToDel.Version.OriginalVersion,
                    _ => apiKey,
                    _ => true,
                    false,
                    Logger);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }

        return false;
    }

    public async Task<(DownloadResourceResult result, bool isSuccess)> RestorePackageAsync(
        PackageIdentity pkgToRestore,
        DownloadResource downloadResource,
        SourceCacheContext sourceCacheContext,
        ISettings settings,
        bool directDownload = false,
        string directDownloadDirectory = null)
    {
        if (pkgToRestore is not null)
            try
            {
                DownloadResourceResult res = await downloadResource.GetDownloadResourceResultAsync(pkgToRestore,
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

    public static async Task GetPackageDependenciesAsync(PackageIdentity package,
                                                  NuGetFramework framework,
                                                  SourceCacheContext cacheContext,
                                                  INuGetLogger logger,
                                                  IEnumerable<SourceRepository> repositories,
                                                  ISet<SourcePackageDependencyInfo> availablePackages)
    {
        if (!availablePackages.Contains(package))
            foreach (SourceRepository sourceRepository in repositories)
            {
                DependencyInfoResource dependencyInfoResource =
                    await sourceRepository.GetResourceAsync<DependencyInfoResource>();

                SourcePackageDependencyInfo dependencyInfo =
                    await dependencyInfoResource.ResolvePackage(package,
                        framework,
                        cacheContext,
                        logger,
                        CancellationToken.None);

                if (dependencyInfo is not null)
                {
                    availablePackages.Add(dependencyInfo);

                    foreach (PackageDependency dependency in dependencyInfo.Dependencies)
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

    public async Task<FileInfo[]> GetNuGetsToPublishOnNuGetOrgAsync(FileInfo[] allNuGets,
                                                                    HashSet<string> excludedPackageNames = null,
                                                                    params string[] packagesToPublish)
    {
        PackageMetadataResource packageMetadataResource = await GetNuGetOrgPackageMetadataResourceAsync();

        return await GetNuGetsToPublishAsync(packageMetadataResource,
            allNuGets,
            excludedPackageNames,
            packagesToPublish);
    }

    public async Task<FileInfo[]> GetNuGetsToPublishAsync(PackageMetadataResource packageMetadataResource,
                                                          FileInfo[] allNuGets,
                                                          HashSet<string> excludedPackageNames = null,
                                                          params string[] packagesToPublish)
    {
        using var sourceCacheContext = new SourceCacheContext();

        (FileInfo nuGet, bool isNotPublished)[] result = await allNuGets.WithWhenAllTasksAsync(x =>
            NuGetNotPublishedAsync(packageMetadataResource,
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
        SourceRepository = NuGetRepository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        PackageMetadataResource = await SourceRepository.GetResourceAsync<PackageMetadataResource>();
        PackageUpdateResource = await SourceRepository.GetResourceAsync<PackageUpdateResource>();
        DownloadResource = await SourceRepository.GetResourceAsync<DownloadResource>();

        await RestorePackageByIdAsync("NuGet.CommandLine");
        //var searchResource = await SourceRepository.GetResourceAsync<PackageSearchResource>();
        //PackageSearchMetadataBuilder.ClonedPackageSearchMetadata[] packages = (await searchResource.SearchAsync(null, new SearchFilter(true), 0, int.MaxValue, Logger, CancellationToken.None)).Where(x => x.IsListed).Cast<PackageSearchMetadataBuilder.ClonedPackageSearchMetadata>().ToArray();
        //var bq = new DirectoryInfo("D:\\_NuGetFeedBq");
        //if (!bq.Exists)
        //{
        //    bq.Create();
        //}

        //PackageIdentity[] allPackages = (await packages.WithWhenAllTasksAsync(GetIdentitiesAsync, true))
        //    .SelectMany(x => x)
        //    .OrderBy(x => x.ToString())
        //    .ToArray();
        //_counter = 0;
        //await allPackages.WithWhenAllTasksAsync(pkg => BackupPackageAsync(pkg, bq, allPackages.Length, Settings, DownloadResource, SourceCacheContext, CancellationToken.None), true);

        //SourceCacheContext.Dispose();
    }

    private async Task<(DownloadResourceResult result, bool isSuccess)> RestorePackageByIdAsync(
        string packageId,
        PackageMetadataResource packageMetadataResource,
        SourceCacheContext sourceCacheContext,
        DownloadResource downloadResource,
        bool includePrerelease = false,
        bool includeUnlisted = false,
        CancellationToken token = default)
    {
        PackageSearchMetadataRegistration[] listedPackages =
            (await packageMetadataResource.GetMetadataAsync(packageId,
                includePrerelease,
                includeUnlisted,
                sourceCacheContext,
                Logger,
                token)).Where(x => x.IsListed)
            .Cast<PackageSearchMetadataRegistration>()
            .ToArray();

        NuGetVersion maxVer = listedPackages.Max(y => y.Version);
        PackageSearchMetadataRegistration latest = listedPackages.First(x => x.Version == maxVer);

        return await RestorePackageAsync(latest.Identity, downloadResource, sourceCacheContext, Settings);
    }

    private async Task<(FileInfo nuGet, bool isNotPublished)> NuGetNotPublishedAsync(
        PackageMetadataResource packageMetadataResource,
        SourceCacheContext sourceCacheContext,
        FileInfo file,
        HashSet<string> excludedPackageNames = null,
        bool includePrerelease = false,
        bool includeUnlisted = false,
        ICollection<string> filterIds = null,
        CancellationToken token = default)
    {
        PackageIdentity identity;

        using (FileStream pkgStream = file.OpenRead())
        {
            using var packageArchiveReader = new PackageArchiveReader(pkgStream);
            identity = packageArchiveReader.NuspecReader.GetIdentity();
        }

        if (excludedPackageNames.IsNotNullOrEmptyCollection()
            && excludedPackageNames.Contains(identity.Id))
            return (file, false);

        PackageSearchMetadataRegistration[] listedPackages = null;

        if (filterIds.IsNullOrEmptyCollection()
            || filterIds.Contains(identity.Id))
            listedPackages =
                (await packageMetadataResource.GetMetadataAsync(identity.Id,
                    includePrerelease,
                    includeUnlisted,
                    sourceCacheContext,
                    Logger,
                    token)).Cast<PackageSearchMetadataRegistration>()
                .ToArray();

        return (file, listedPackages?.All(x => !x.Version.Equals(identity.Version)) ?? false);
    }
}