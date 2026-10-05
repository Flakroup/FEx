using FEx.Agnostics.Abstractions.Interfaces;
using FEx.EFCore.Configuration;
using FEx.Imaging.Windows.Model;
using NSubstitute;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace FEx.Imaging.Windows.Tests;

public sealed class ConfigTests : ImagingTestBase
{
    [Fact]
    public void FilesCacheServiceConfig_CreatesTheCacheDirectoryAndExposesItsSettings()
    {
        var dir = Path.Combine(Dir, "nested", "cache");
        var dbConfig = Substitute.For<IDbServiceConfig>();

        var sut = new FilesCacheServiceConfig(dbConfig, new(dir), true, TimeSpan.FromDays(2), false);

        Directory.Exists(dir).ShouldBeTrue();
        sut.FilesCacheDir.FullName.ShouldBe(Path.GetFullPath(dir));
        sut.FilesCacheDirPath.ShouldBe(sut.FilesCacheDir.FullName);
        sut.DbServiceConfig.ShouldBeSameAs(dbConfig);
        sut.UseHttpClientService.ShouldBeTrue();
        sut.CacheValidPeriod.ShouldBe(TimeSpan.FromDays(2));
        sut.CacheAll.ShouldBeFalse();
    }

    [Fact]
    public void FilesCacheServiceConfig_Defaults_AreNoHttpClientServiceNoExpiryAndCacheAll()
    {
        var sut = new FilesCacheServiceConfig(Substitute.For<IDbServiceConfig>(), new(Dir));

        sut.UseHttpClientService.ShouldBeFalse();
        sut.CacheValidPeriod.ShouldBeNull();
        sut.CacheAll.ShouldBeTrue();
    }

    [Fact]
    public void FilesCacheServiceConfigurator_Defaults_CacheAllAndLeaveTheRestUnset()
    {
        var sut = new FilesCacheServiceConfigurator();

        sut.CacheAll.ShouldBeTrue();
        sut.UseSqlite.ShouldBeFalse();
        sut.UseHttpClientService.ShouldBeFalse();
        sut.CacheValidPeriod.ShouldBeNull();
        sut.ImageCache.ShouldBeNull();
        sut.SqlDbName.ShouldBeNull();
        sut.SqliteDbFileName.ShouldBeNull();
    }

    [Fact]
    public void FExDefaultFilesCacheServiceConfig_ExplicitSettings_AreTakenFromTheConfigurator()
    {
        var cacheDir = new DirectoryInfo(Path.Combine(Dir, "explicit"));
        var configurator = Substitute.For<IFilesCacheServiceConfigurator>();
        configurator.ImageCache.Returns(cacheDir);
        configurator.SqliteDbFileName.Returns("custom.db");
        configurator.SqlDbName.Returns("CustomImageCache");
        configurator.UseSqlite.Returns(true);
        configurator.UseHttpClientService.Returns(true);
        configurator.CacheValidPeriod.Returns(TimeSpan.FromHours(1));
        configurator.CacheAll.Returns(false);

        var sut = new FExDefaultFilesCacheServiceConfig(Substitute.For<IAppInfoProvider>(), configurator);

        sut.FilesCacheDir.FullName.ShouldBe(cacheDir.FullName);
        sut.UseHttpClientService.ShouldBeTrue();
        sut.CacheValidPeriod.ShouldBe(TimeSpan.FromHours(1));
        sut.CacheAll.ShouldBeFalse();
        var dbConfig = sut.DbServiceConfig.DbConfig;
        dbConfig.SqlDbName.ShouldBe("CustomImageCache");
        dbConfig.UseSqlite.ShouldBeTrue();
        dbConfig.SqliteDbFile.ShouldNotBeNull().FullName.ShouldBe(Path.Combine(cacheDir.FullName, "custom.db"));
        dbConfig.DropIfMigrationFailed.ShouldBeTrue();
    }

    [Fact]
    public void FExDefaultFilesCacheServiceConfig_NoImageCache_FallsBackToTheAppDataImageCacheFolder()
    {
        var appData = new DirectoryInfo(Path.Combine(Dir, "appdata"));
        var appInfo = Substitute.For<IAppInfoProvider>();
        appInfo.AppData.Returns(appData);
        var configurator = Substitute.For<IFilesCacheServiceConfigurator>();
        configurator.SqlDbName.Returns("FallbackImageCache");
        configurator.ImageCache.Returns((DirectoryInfo)null!);
        configurator.SqliteDbFileName.Returns((string)null!);

        var sut = new FExDefaultFilesCacheServiceConfig(appInfo, configurator);

        sut.FilesCacheDir.FullName.ShouldBe(Path.Combine(appData.FullName, "ImageCache"));
        sut.DbServiceConfig.DbConfig.SqliteDbFile.ShouldNotBeNull().FullName
            .ShouldBe(Path.Combine(appData.FullName, "ImageCache", "IndexEF.db"));
    }

    [Fact]
    public void DoesCacheExists_NullEntry_IsFalse()
    {
        Extensions.DoesCacheExists(null!).ShouldBeFalse();
    }

    [Fact]
    public void DoesCacheExists_EntryWithoutFile_IsFalse()
    {
        using var entry = new IndexEntry();

        entry.DoesCacheExists().ShouldBeFalse();
    }

    [Fact]
    public void DoesCacheExists_EntryWithItsFile_IsTrueUntilTheFileIsDeleted()
    {
        var path = WriteFile("a.png", Png(2, 2));
        using var entry = new IndexEntry("https://images.test/a.png", CreateConfig(), Path.GetFileName(path));

        entry.DoesCacheExists().ShouldBeTrue();

        File.Delete(path);

        entry.DoesCacheExists().ShouldBeFalse();
    }
}
