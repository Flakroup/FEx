using System;

namespace FEx.Sqlx.Abstractions;

/// <summary>Read-only description of a SQL Server instance, free of SMO/WMI types.</summary>
public interface ISqlInstanceInfo
{
    string SQLInstance { get; }
    Version? BuildClrVersion { get; }
    string? Collation { get; }
    int? CollationID { get; }
    int? ComparisonStyle { get; }
    string? ComputerNamePhysicalNetBIOS { get; }
    string? Edition { get; }
    string? EditionID { get; }
    string? EngineEdition { get; }
    SqlFileStreamLevel? FilestreamConfiguredLevel { get; }
    SqlFileStreamLevel? FilestreamEffectiveLevel { get; }
    string? FilestreamShareName { get; }
    SqlHadrManagerStatus? HadrManagerStatus { get; }
    string? InstanceDefaultBackupPath { get; }
    string? InstanceDefaultDataPath { get; }
    string? InstanceDefaultLogPath { get; }
    string? InstanceName { get; }
    bool? IsAdvancedAnalyticsInstalled { get; }
    bool? IsBigDataCluster { get; }
    bool? IsClustered { get; }
    bool? IsFullTextInstalled { get; }
    bool? IsHadrEnabled { get; }
    bool? IsIntegratedSecurityOnly { get; }
    bool IsLocalDB { get; }
    bool? IsPolyBaseInstalled { get; }
    bool? IsSingleUser { get; }
    bool? IsTempDbMetadataMemoryOptimized { get; }
    bool? IsXTPSupported { get; }
    int? LCID { get; }
    string? LicenseType { get; }
    string? MachineName { get; }
    string? NumLicenses { get; }
    int? ProcessID { get; }
    string? ProductBuild { get; }
    string? ProductBuildType { get; }
    string? ProductLevel { get; }
    string? ProductMajorVersion { get; }
    string? ProductMinorVersion { get; }
    string? ProductUpdateLevel { get; }
    string? ProductUpdateReference { get; }
    Version? ProductVersion { get; }
    DateTime? ResourceLastUpdateDateTime { get; }
    Version? ResourceVersion { get; }
    string? ServerName { get; }
    short? SqlCharSet { get; }
    string? SqlCharSetName { get; }
    short? SqlSortOrder { get; }
    string? SqlSortOrderName { get; }
    SqlLoginMode LoginMode { get; }
}
