using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions.Extensions;
using FEx.Sqlx.Enums;
using FEx.Sqlx.Extensions;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
#if NETFRAMEWORK
using Microsoft.SqlServer.Management.Smo.Wmi;
#endif

namespace FEx.Sqlx;

// CS0618: System.Data.SqlClient.SqlConnection is obsolete in favour of Microsoft.Data.SqlClient.
// Retained for backward compatibility; full migration tracked as tech debt.
#pragma warning disable CS0618

public class SQLInstanceInfo
{
    public string SQLInstance { get; }
    public Version? BuildClrVersion { get; protected set; }
    public string? Collation { get; protected set; }
    public int? CollationID { get; protected set; }
    public int? ComparisonStyle { get; protected set; }
    public string? ComputerNamePhysicalNetBIOS { get; protected set; }
    public string? Edition { get; protected set; }
    public string? EditionID { get; protected set; }
    public string? EngineEdition { get; protected set; }
    public FileStreamEffectiveLevel? FilestreamConfiguredLevel { get; protected set; }
    public FileStreamEffectiveLevel? FilestreamEffectiveLevel { get; protected set; }
    public string? FilestreamShareName { get; protected set; }
    public HadrManagerStatus? HadrManagerStatus { get; protected set; }
    public string? InstanceDefaultBackupPath { get; protected set; }
    public string? InstanceDefaultDataPath { get; protected set; }
    public string? InstanceDefaultLogPath { get; protected set; }
    public string? InstanceName { get; protected set; }
    public bool? IsAdvancedAnalyticsInstalled { get; protected set; }
    public bool? IsBigDataCluster { get; protected set; }
    public bool? IsClustered { get; protected set; }
    public bool? IsFullTextInstalled { get; protected set; }
    public bool? IsHadrEnabled { get; protected set; }
    public bool? IsIntegratedSecurityOnly { get; protected set; }
    public bool IsLocalDB { get; protected set; }
    public bool? IsPolyBaseInstalled { get; protected set; }
    public bool? IsSingleUser { get; protected set; }
    public bool? IsTempDbMetadataMemoryOptimized { get; protected set; }
    public bool? IsXTPSupported { get; protected set; }
    public int? LCID { get; protected set; }
    public string? LicenseType { get; protected set; }
    public string? MachineName { get; protected set; }
    public string? NumLicenses { get; protected set; }
    public int? ProcessID { get; protected set; }
    public string? ProductBuild { get; protected set; }
    public string? ProductBuildType { get; protected set; }
    public string? ProductLevel { get; protected set; }
    public string? ProductMajorVersion { get; protected set; }
    public string? ProductMinorVersion { get; protected set; }
    public string? ProductUpdateLevel { get; protected set; }
    public string? ProductUpdateReference { get; protected set; }
    public Version? ProductVersion { get; protected set; }
    public DateTime? ResourceLastUpdateDateTime { get; protected set; }
    public Version? ResourceVersion { get; protected set; }
    public string? ServerName { get; protected set; }
    public short? SqlCharSet { get; protected set; }
    public string? SqlCharSetName { get; protected set; }
    public short? SqlSortOrder { get; protected set; }
    public string? SqlSortOrderName { get; protected set; }
    public ServerLoginMode LoginMode => Server?.LoginMode ?? ServerLoginMode.Unknown;

    protected Server? Server { get; set; }

#if NETFRAMEWORK
    public SQLInstanceInfo(ServerInstance serverInstance, ManagedComputer comp)
    {
        const string defaultInstance = "MSSQLSERVER";

        SQLInstance = serverInstance.Name == defaultInstance
            ? comp.Name
            : $"{comp.Name}\\{serverInstance.Name}";
    }
#endif

    public SQLInstanceInfo(string instanceName)
    {
        SQLInstance = instanceName;
    }

    public async Task<bool> LoadInfoAsync()
    {
        Dictionary<ServerProp, string?>? props = null;

        try
        {
            var connStr =
                $"Data Source={SQLInstance};Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
#if NETFRAMEWORK
            using var conn = new SqlConnection(connStr);
#else
            await using var conn = new SqlConnection(connStr);
#endif
            await conn.OpenAsync();
            var result = await conn.RunSqlAsync(SqlConnectionExtensions.PropsSQL);

            props = result.ToDictionary(
                x => (ServerProp)Enum.Parse(typeof(ServerProp), Convert.ToString(x["propertyname"]).Guard("propertyname")),
                // ReSharper disable once RedundantCast - required on down-level TFMs where Convert.ToString returns oblivious 'string'
                x => (string?)Convert.ToString(x["propertyvalue"]));
        }
        catch
        {
            //ignored
        }

        try
        {
            Server = new(SQLInstance);
        }
        catch
        {
            //ignored
        }

        if (props is not null)
            ProcessProps(props);

        return true;
    }

    public bool LoadInfo()
    {
        Dictionary<ServerProp, string?>? props = null;

        try
        {
            using var conn =
                new SqlConnection(
                    $"Data Source={SQLInstance};Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

            conn.Open();
            var result = conn.RunSql(SqlConnectionExtensions.PropsSQL);

            props = result.ToDictionary(
                x => (ServerProp)Enum.Parse(typeof(ServerProp), Convert.ToString(x["propertyname"]).Guard("propertyname")),
                // ReSharper disable once RedundantCast - required on down-level TFMs where Convert.ToString returns oblivious 'string'
                x => (string?)Convert.ToString(x["propertyvalue"]));
        }
        catch
        {
            //ignored
        }

        try
        {
            Server = new(SQLInstance);
        }
        catch
        {
            //ignored
        }

        if (props is not null)
            ProcessProps(props);

        return true;
    }

    private static void SafePropertySet<T>(Action<T> propSet, Func<T> valueGet, Func<T>? serverValueGet = null)
    {
        var failed = false;
        // Unconstrained generic: default may be null for reference T; guarded by the `result is null` check below.
        T result = default!;

        if (serverValueGet is not null)
            try
            {
                result = serverValueGet();
                propSet(result);
            }
            catch
            {
                failed = true;
                //ignored
            }

        if (serverValueGet is null
            || failed
            || result is null)
            try
            {
                propSet(valueGet());
            }
            catch
            {
                //ignored
            }
    }

    private static string? GetServerEngineEdition(int? value) =>
        value.HasValue
            ? SqlConnectionExtensions.ServerEngineEditions.ForwardIndex[value.Value]
            : null;

    private static string? GetServerEditionID(long? value) =>
        value.HasValue
            ? SqlConnectionExtensions.ServerEditionIDs.ForwardIndex[value.Value]
            : null;

    private static short? GetShort(IDictionary<ServerProp, string?> props, ServerProp sP)
    {
        var stringValue = props.TryGetKeyValue(sP);

        return stringValue.IsNotNullOrEmptyString()
            ? Convert.ToInt16(stringValue)
            : null;
    }

    private static int? GetInt(IDictionary<ServerProp, string?> props, ServerProp sP)
    {
        var stringValue = props.TryGetKeyValue(sP);

        return stringValue.IsNotNullOrEmptyString()
            ? Convert.ToInt32(stringValue)
            : null;
    }

    private static long? GetLong(IDictionary<ServerProp, string?> props, ServerProp sP)
    {
        var stringValue = props.TryGetKeyValue(sP);

        return stringValue.IsNotNullOrEmptyString()
            ? Convert.ToInt64(stringValue)
            : null;
    }

    private static Version? GetVersion(IDictionary<ServerProp, string?> props, ServerProp sP)
    {
        var stringValue = props.TryGetKeyValue(sP);

        return stringValue is not null
            ? Version.Parse(stringValue)
            : null;
    }

    private static bool? GetBoolFromInt(IDictionary<ServerProp, string?> props, ServerProp sP)
    {
        var intValue = GetInt(props, sP);

        return intValue.HasValue
            ? Convert.ToBoolean(intValue.Value)
            : null;
    }

    private static DateTime? GetDateTime(IDictionary<ServerProp, string?> props, ServerProp sP)
    {
        var stringValue = props.TryGetKeyValue(sP);

        return stringValue is not null
            ? DateTime.Parse(stringValue)
            : null;
    }

    private void ProcessProps(IDictionary<ServerProp, string?> props)
    {
        try
        {
            SafePropertySet(x => BuildClrVersion = x,
                () => Version.Parse(props.TryGetKeyValue(ServerProp.BuildClrVersion).Guard(nameof(ServerProp.BuildClrVersion)).TrimStart('v', '.')),
                () => Server?.BuildClrVersion);

            SafePropertySet(x => Collation = x,
                () => props.TryGetKeyValue(ServerProp.Collation),
                () => Server?.Collation);

            SafePropertySet(x => CollationID = x,
                () => GetInt(props, ServerProp.CollationID),
                () => Server?.CollationID);

            SafePropertySet(x => ComparisonStyle = x,
                () => GetInt(props, ServerProp.ComparisonStyle),
                () => Server?.ComparisonStyle);

            SafePropertySet(x => ComputerNamePhysicalNetBIOS = x,
                () => props.TryGetKeyValue(ServerProp.ComputerNamePhysicalNetBIOS),
                () => Server?.ComputerNamePhysicalNetBIOS);

            SafePropertySet(x => Edition = x, () => props.TryGetKeyValue(ServerProp.Edition), () => Server?.Edition);
            SafePropertySet(x => EditionID = x, () => GetServerEditionID(GetLong(props, ServerProp.EditionID)));

            SafePropertySet(x => EngineEdition = x,
                () => GetServerEngineEdition(GetInt(props, ServerProp.EngineEdition)),
                () => GetServerEngineEdition((int?)Server?.EngineEdition));

            SafePropertySet(x => FilestreamConfiguredLevel = x,
                () => (FileStreamEffectiveLevel?)GetInt(props, ServerProp.FilestreamConfiguredLevel));

            SafePropertySet(x => FilestreamEffectiveLevel = x,
                () => (FileStreamEffectiveLevel?)GetInt(props, ServerProp.FilestreamEffectiveLevel),
                () => Server?.FilestreamLevel);

            SafePropertySet(x => FilestreamShareName = x,
                () => props.TryGetKeyValue(ServerProp.FilestreamShareName),
                () => Server?.FilestreamShareName);

            SafePropertySet(x => HadrManagerStatus = x,
                () => (HadrManagerStatus?)GetInt(props, ServerProp.HadrManagerStatus),
                () => Server?.HadrManagerStatus);

            SafePropertySet(x => InstanceDefaultBackupPath = x,
                () => props.TryGetKeyValue(ServerProp.InstanceDefaultBackupPath));

            SafePropertySet(x => InstanceDefaultDataPath = x,
                () => props.TryGetKeyValue(ServerProp.InstanceDefaultDataPath));

            SafePropertySet(x => InstanceDefaultLogPath = x,
                () => props.TryGetKeyValue(ServerProp.InstanceDefaultLogPath));

            SafePropertySet(x => InstanceName = x,
                () => props.TryGetKeyValue(ServerProp.InstanceName),
                () => Server?.InstanceName);

            SafePropertySet(x => IsAdvancedAnalyticsInstalled = x,
                () => GetBoolFromInt(props, ServerProp.IsAdvancedAnalyticsInstalled));

            SafePropertySet(x => IsBigDataCluster = x, () => GetBoolFromInt(props, ServerProp.IsBigDataCluster));

            SafePropertySet(x => IsClustered = x,
                () => GetBoolFromInt(props, ServerProp.IsClustered),
                () => Server?.IsClustered);

            SafePropertySet(x => IsFullTextInstalled = x,
                () => GetBoolFromInt(props, ServerProp.IsFullTextInstalled),
                () => Server?.IsFullTextInstalled);

            SafePropertySet(x => IsHadrEnabled = x,
                () => GetBoolFromInt(props, ServerProp.IsHadrEnabled),
                () => Server?.IsHadrEnabled);

            SafePropertySet(x => IsIntegratedSecurityOnly = x,
                () => GetBoolFromInt(props, ServerProp.IsIntegratedSecurityOnly),
                () => Server?.LoginMode == ServerLoginMode.Integrated);

            SafePropertySet(x => IsLocalDB = x ?? false, () => GetBoolFromInt(props, ServerProp.IsLocalDB));

            SafePropertySet(x => IsPolyBaseInstalled = x,
                () => GetBoolFromInt(props, ServerProp.IsPolyBaseInstalled),
                () => Server?.IsPolyBaseInstalled);

            SafePropertySet(x => IsSingleUser = x,
                () => GetBoolFromInt(props, ServerProp.IsSingleUser),
                () => Server?.IsSingleUser);

            SafePropertySet(x => IsTempDbMetadataMemoryOptimized = x,
                () => GetBoolFromInt(props, ServerProp.IsTempDbMetadataMemoryOptimized));

            SafePropertySet(x => IsXTPSupported = x,
                () => GetBoolFromInt(props, ServerProp.IsXTPSupported),
                () => Server?.IsXTPSupported);

            SafePropertySet(x => LCID = x, () => GetInt(props, ServerProp.LCID));
            SafePropertySet(x => LicenseType = x, () => props.TryGetKeyValue(ServerProp.LicenseType));
            SafePropertySet(x => MachineName = x, () => props.TryGetKeyValue(ServerProp.MachineName));
            SafePropertySet(x => NumLicenses = x, () => props.TryGetKeyValue(ServerProp.NumLicenses));
            SafePropertySet(x => ProcessID = x, () => GetInt(props, ServerProp.ProcessID));
            SafePropertySet(x => ProductBuild = x, () => props.TryGetKeyValue(ServerProp.ProductBuild));
            SafePropertySet(x => ProductBuildType = x, () => props.TryGetKeyValue(ServerProp.ProductBuildType));

            SafePropertySet(x => ProductLevel = x,
                () => props.TryGetKeyValue(ServerProp.ProductLevel),
                () => Server?.ProductLevel);

            SafePropertySet(x => ProductMajorVersion = x, () => props.TryGetKeyValue(ServerProp.ProductMajorVersion));
            SafePropertySet(x => ProductMinorVersion = x, () => props.TryGetKeyValue(ServerProp.ProductMinorVersion));

            SafePropertySet(x => ProductUpdateLevel = x,
                () => props.TryGetKeyValue(ServerProp.ProductUpdateLevel),
                () => Server?.ProductUpdateLevel);

            SafePropertySet(x => ProductUpdateReference = x,
                () => props.TryGetKeyValue(ServerProp.ProductUpdateReference));

            SafePropertySet(x => ProductVersion = x, () => GetVersion(props, ServerProp.ProductVersion));

            SafePropertySet(x => ResourceLastUpdateDateTime = x,
                () => GetDateTime(props, ServerProp.ResourceLastUpdateDateTime),
                () => Server?.ResourceLastUpdateDateTime);

            SafePropertySet(x => ResourceVersion = x,
                () => GetVersion(props, ServerProp.ResourceVersion),
                () => Server?.ResourceVersion);

            SafePropertySet(x => ServerName = x, () => props.TryGetKeyValue(ServerProp.ServerName));

            SafePropertySet(x => SqlCharSet = x,
                () => GetShort(props, ServerProp.SqlCharSet),
                () => Server?.SqlCharSet);

            SafePropertySet(x => SqlCharSetName = x,
                () => props.TryGetKeyValue(ServerProp.SqlCharSetName),
                () => Server?.SqlCharSetName);

            SafePropertySet(x => SqlSortOrder = x,
                () => GetShort(props, ServerProp.SqlSortOrder),
                () => Server?.SqlSortOrder);

            SafePropertySet(x => SqlSortOrderName = x,
                () => props.TryGetKeyValue(ServerProp.SqlSortOrderName),
                () => Server?.SqlSortOrderName);
        }
        catch (Exception ex)
        {
            ex.HandleException(false);
        }
    }
}