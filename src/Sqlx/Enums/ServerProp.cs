using System.ComponentModel;

namespace FEx.Sqlx.Enums;

public enum ServerProp
{
    [Description("BuildClrVersion")]
    BuildClrVersion,

    [Description("Collation")]
    Collation,

    [Description("CollationID")]
    CollationID,

    [Description("ComparisonStyle")]
    ComparisonStyle,

    [Description("ComputerNamePhysicalNetBIOS")]
    ComputerNamePhysicalNetBIOS,

    [Description("Edition")]
    Edition,

    [Description("EditionID")]
    EditionID,

    [Description("EngineEdition")]
    EngineEdition,

    [Description("FilestreamConfiguredLevel")]
    FilestreamConfiguredLevel,

    [Description("FilestreamEffectiveLevel")]
    FilestreamEffectiveLevel,

    [Description("FilestreamShareName")]
    FilestreamShareName,

    [Description("HadrManagerStatus")]
    HadrManagerStatus,

    [Description("InstanceDefaultBackupPath")]
    InstanceDefaultBackupPath,

    [Description("InstanceDefaultDataPath")]
    InstanceDefaultDataPath,

    [Description("InstanceDefaultLogPath")]
    InstanceDefaultLogPath,

    [Description("InstanceName")]
    InstanceName,

    [Description("IsAdvancedAnalyticsInstalled")]
    IsAdvancedAnalyticsInstalled,

    [Description("IsBigDataCluster")]
    IsBigDataCluster,

    [Description("IsClustered")]
    IsClustered,

    [Description("IsFullTextInstalled")]
    IsFullTextInstalled,

    [Description("IsHadrEnabled")]
    IsHadrEnabled,

    [Description("IsIntegratedSecurityOnly")]
    IsIntegratedSecurityOnly,

    [Description("IsLocalDB")]
    IsLocalDB,

    [Description("IsPolyBaseInstalled")]
    IsPolyBaseInstalled,

    [Description("IsSingleUser")]
    IsSingleUser,

    [Description("IsTempDbMetadataMemoryOptimized")]
    IsTempDbMetadataMemoryOptimized,

    [Description("IsXTPSupported")]
    IsXTPSupported,

    [Description("LCID")]
    LCID,

    [Description("LicenseType")]
    LicenseType,

    [Description("MachineName")]
    MachineName,

    [Description("NumLicenses")]
    NumLicenses,

    [Description("ProcessID")]
    ProcessID,

    [Description("ProductBuild")]
    ProductBuild,

    [Description("ProductBuildType")]
    ProductBuildType,

    [Description("ProductLevel")]
    ProductLevel,

    [Description("ProductMajorVersion")]
    ProductMajorVersion,

    [Description("ProductMinorVersion")]
    ProductMinorVersion,

    [Description("ProductUpdateLevel")]
    ProductUpdateLevel,

    [Description("ProductUpdateReference")]
    ProductUpdateReference,

    [Description("ProductVersion")]
    ProductVersion,

    [Description("ResourceLastUpdateDateTime")]
    ResourceLastUpdateDateTime,

    [Description("ResourceVersion")]
    ResourceVersion,

    [Description("ServerName")]
    ServerName,

    [Description("SqlCharSet")]
    SqlCharSet,

    [Description("SqlCharSetName")]
    SqlCharSetName,

    [Description("SqlSortOrder")]
    SqlSortOrder,

    [Description("SqlSortOrderName")]
    SqlSortOrderName
}