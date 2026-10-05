using FEx.Sqlx.Enums;
using Microsoft.SqlServer.Management.Smo;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Sqlx.Tests;

/// <summary>Mapping of the SERVERPROPERTY values onto <see cref="SQLInstanceInfo" />; no server is contacted.</summary>
public sealed class SQLInstanceInfoTests
{
    private static readonly Dictionary<ServerProp, string?> FullProps = new()
    {
        [ServerProp.BuildClrVersion] = "v4.0.30319",
        [ServerProp.Collation] = "Latin1_General_CI_AS",
        [ServerProp.CollationID] = "872468488",
        [ServerProp.ComparisonStyle] = "196609",
        [ServerProp.ComputerNamePhysicalNetBIOS] = "HOST1",
        [ServerProp.Edition] = "Developer Edition (64-bit)",
        [ServerProp.EditionID] = "-2117995310",
        [ServerProp.EngineEdition] = "3",
        [ServerProp.FilestreamConfiguredLevel] = "2",
        [ServerProp.FilestreamEffectiveLevel] = "1",
        [ServerProp.FilestreamShareName] = "MSSQLSERVER",
        [ServerProp.HadrManagerStatus] = "1",
        [ServerProp.InstanceDefaultBackupPath] = @"C:\Backup",
        [ServerProp.InstanceDefaultDataPath] = @"C:\Data",
        [ServerProp.InstanceDefaultLogPath] = @"C:\Log",
        [ServerProp.InstanceName] = "SQL2022",
        [ServerProp.IsAdvancedAnalyticsInstalled] = "1",
        [ServerProp.IsBigDataCluster] = "0",
        [ServerProp.IsClustered] = "1",
        [ServerProp.IsFullTextInstalled] = "1",
        [ServerProp.IsHadrEnabled] = "0",
        [ServerProp.IsIntegratedSecurityOnly] = "1",
        [ServerProp.IsLocalDB] = "1",
        [ServerProp.IsPolyBaseInstalled] = "0",
        [ServerProp.IsSingleUser] = "1",
        [ServerProp.IsTempDbMetadataMemoryOptimized] = "1",
        [ServerProp.IsXTPSupported] = "1",
        [ServerProp.LCID] = "1033",
        [ServerProp.LicenseType] = "DISABLED",
        [ServerProp.MachineName] = "HOST1",
        [ServerProp.NumLicenses] = "n/a",
        [ServerProp.ProcessID] = "1234",
        [ServerProp.ProductBuild] = "1000",
        [ServerProp.ProductBuildType] = "RTM",
        [ServerProp.ProductLevel] = "RTM",
        [ServerProp.ProductMajorVersion] = "16",
        [ServerProp.ProductMinorVersion] = "0",
        [ServerProp.ProductUpdateLevel] = "CU1",
        [ServerProp.ProductUpdateReference] = "KB123",
        [ServerProp.ProductVersion] = "16.0.1000.6",
        [ServerProp.ResourceLastUpdateDateTime] = "2024-03-05T10:20:30",
        [ServerProp.ResourceVersion] = "16.0.1000",
        [ServerProp.ServerName] = @"HOST1\SQL2022",
        [ServerProp.SqlCharSet] = "1",
        [ServerProp.SqlCharSetName] = "iso_1",
        [ServerProp.SqlSortOrder] = "52",
        [ServerProp.SqlSortOrderName] = "nocase_iso"
    };

    [Fact]
    public void Constructor_KeepsTheInstanceName() =>
        new SQLInstanceInfo(@"HOST\SQL").SQLInstance.ShouldBe(@"HOST\SQL");

    [Fact]
    public void Constructor_LeavesEveryPropertyUnloaded()
    {
        var info = new SQLInstanceInfo("HOST");

        info.Edition.ShouldBeNull();
        info.ProductVersion.ShouldBeNull();
        info.IsLocalDB.ShouldBeFalse();
        info.LoginMode.ShouldBe(ServerLoginMode.Unknown);
    }

    [Fact]
    public void ProcessProps_MapsTextValuesVerbatim()
    {
        var info = Process(FullProps);

        info.Collation.ShouldBe("Latin1_General_CI_AS");
        info.ComputerNamePhysicalNetBIOS.ShouldBe("HOST1");
        info.Edition.ShouldBe("Developer Edition (64-bit)");
        info.FilestreamShareName.ShouldBe("MSSQLSERVER");
        info.InstanceDefaultBackupPath.ShouldBe(@"C:\Backup");
        info.InstanceDefaultDataPath.ShouldBe(@"C:\Data");
        info.InstanceDefaultLogPath.ShouldBe(@"C:\Log");
        info.InstanceName.ShouldBe("SQL2022");
        info.LicenseType.ShouldBe("DISABLED");
        info.MachineName.ShouldBe("HOST1");
        info.NumLicenses.ShouldBe("n/a");
        info.ProductBuild.ShouldBe("1000");
        info.ProductBuildType.ShouldBe("RTM");
        info.ProductLevel.ShouldBe("RTM");
        info.ProductMajorVersion.ShouldBe("16");
        info.ProductMinorVersion.ShouldBe("0");
        info.ProductUpdateLevel.ShouldBe("CU1");
        info.ProductUpdateReference.ShouldBe("KB123");
        info.ServerName.ShouldBe(@"HOST1\SQL2022");
        info.SqlCharSetName.ShouldBe("iso_1");
        info.SqlSortOrderName.ShouldBe("nocase_iso");
    }

    [Fact]
    public void ProcessProps_ParsesNumbersVersionsAndDates()
    {
        var info = Process(FullProps);

        info.CollationID.ShouldBe(872468488);
        info.ComparisonStyle.ShouldBe(196609);
        info.LCID.ShouldBe(1033);
        info.ProcessID.ShouldBe(1234);
        info.SqlCharSet.ShouldBe((short)1);
        info.SqlSortOrder.ShouldBe((short)52);
        info.ProductVersion.ShouldBe(new Version(16, 0, 1000, 6));
        info.ResourceVersion.ShouldBe(new Version(16, 0, 1000));
        info.BuildClrVersion.ShouldBe(new Version(4, 0, 30319));
        info.ResourceLastUpdateDateTime.ShouldBe(new DateTime(2024, 3, 5, 10, 20, 30));
    }

    [Fact]
    public void ProcessProps_ConvertsIntegerFlagsToBooleans()
    {
        var info = Process(FullProps);

        info.IsAdvancedAnalyticsInstalled.ShouldBe(true);
        info.IsBigDataCluster.ShouldBe(false);
        info.IsClustered.ShouldBe(true);
        info.IsFullTextInstalled.ShouldBe(true);
        info.IsHadrEnabled.ShouldBe(false);
        info.IsLocalDB.ShouldBeTrue();
        info.IsPolyBaseInstalled.ShouldBe(false);
        info.IsSingleUser.ShouldBe(true);
        info.IsTempDbMetadataMemoryOptimized.ShouldBe(true);
        info.IsXTPSupported.ShouldBe(true);
    }

    [Fact]
    public void ProcessProps_ResolvesEditionAndEngineEditionDescriptions()
    {
        var info = Process(FullProps);

        info.EditionID.ShouldBe("Developer");
        info.EngineEdition.ShouldBe("Enterprise");
    }

    [Fact]
    public void ProcessProps_MapsEnumValues()
    {
        var info = Process(FullProps);

        info.FilestreamConfiguredLevel.ShouldBe((FileStreamEffectiveLevel)2);
        info.FilestreamEffectiveLevel.ShouldBe((FileStreamEffectiveLevel)1);
        info.HadrManagerStatus.ShouldBe((HadrManagerStatus)1);
    }

    [Fact]
    public void ProcessProps_ReportsIntegratedSecurityOnlyAsFalse_WhenNoServerIsAttached() =>
        Process(FullProps).IsIntegratedSecurityOnly.ShouldBe(false);

    [Fact]
    public void ProcessProps_StripsLeadingMarkersFromTheClrVersion()
    {
        var props = new Dictionary<ServerProp, string?> { [ServerProp.BuildClrVersion] = "v.4.0.30319" };

        Process(props).BuildClrVersion.ShouldBe(new Version(4, 0, 30319));
    }

    [Fact]
    public void ProcessProps_LeavesPropertiesUnset_WhenTheValuesAreMissingOrEmpty()
    {
        var props = new Dictionary<ServerProp, string?>
        {
            [ServerProp.CollationID] = string.Empty,
            [ServerProp.IsClustered] = null
        };

        var info = Process(props);

        info.CollationID.ShouldBeNull();
        info.IsClustered.ShouldBeNull();
        info.Edition.ShouldBeNull();
        info.EditionID.ShouldBeNull();
        info.EngineEdition.ShouldBeNull();
        info.ProductVersion.ShouldBeNull();
        info.ResourceLastUpdateDateTime.ShouldBeNull();
        info.SqlCharSet.ShouldBeNull();
        info.IsLocalDB.ShouldBeFalse();
    }

    [Fact]
    public void ProcessProps_SkipsOnlyTheUnparsableProperty()
    {
        var props = new Dictionary<ServerProp, string?>
        {
            [ServerProp.CollationID] = "not-a-number",
            [ServerProp.ProductVersion] = "not-a-version",
            [ServerProp.EditionID] = "42",
            [ServerProp.Collation] = "Polish_CI_AS"
        };

        var info = Process(props);

        info.CollationID.ShouldBeNull();
        info.ProductVersion.ShouldBeNull();
        info.EditionID.ShouldBeNull();
        info.Collation.ShouldBe("Polish_CI_AS");
    }

    [Fact]
    public async Task LoadInfoAsync_ReturnsTrueAndKeepsPropertiesUnset_WhenTheConnectionStringIsRejected()
    {
        var info = new SQLInstanceInfo(UnreachableInstance);

        (await info.LoadInfoAsync()).ShouldBeTrue();

        info.Edition.ShouldBeNull();
        info.ProductVersion.ShouldBeNull();
    }

    [Fact]
    public void LoadInfo_ReturnsTrueAndKeepsPropertiesUnset_WhenTheConnectionStringIsRejected()
    {
        var info = new SQLInstanceInfo(UnreachableInstance);

        info.LoadInfo().ShouldBeTrue();

        info.Edition.ShouldBeNull();
        info.ProductVersion.ShouldBeNull();
    }

    // An unknown connection-string keyword makes the SqlConnection constructor throw, so no connection attempt is made at all.
    private const string UnreachableInstance = "host;NoSuchKeyword=1";

    private static SQLInstanceInfo Process(IDictionary<ServerProp, string?> props)
    {
        var info = new SQLInstanceInfo("HOST");

        typeof(SQLInstanceInfo)
            .GetMethod("ProcessProps", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(info, [props]);

        return info;
    }
}
