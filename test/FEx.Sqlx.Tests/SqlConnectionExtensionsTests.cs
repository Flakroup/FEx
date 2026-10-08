using FEx.Sqlx.Enums;
using FEx.Sqlx.Extensions;
using Microsoft.Data.SqlClient;
using Shouldly;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Sqlx.Tests;

/// <summary>The static lookup maps and the generated server-properties query; no server is contacted.</summary>
public sealed class SqlConnectionExtensionsTests
{
    private static string PropsSql => SqlConnectionExtensions.PropsSQL;

    [Fact]
    public void ServerProps_HasAnEntryForEveryEnumMember() =>
        SqlConnectionExtensions.ServerProps.ForwardIndex.Count.ShouldBe(Enum.GetValues(typeof(ServerProp)).Length);

    [Fact]
    public void ServerProps_MapsTheServerPropertyNameToTheEnumMember()
    {
        SqlConnectionExtensions.ServerProps.ForwardIndex["EngineEdition"].ShouldBe(ServerProp.EngineEdition);
        SqlConnectionExtensions.ServerProps.ForwardIndex["IsLocalDB"].ShouldBe(ServerProp.IsLocalDB);
    }

    [Fact]
    public void ServerEditionIDs_MapsTheNumericEditionIdToItsDescription()
    {
        SqlConnectionExtensions.ServerEditionIDs.ForwardIndex[1804890536L].ShouldBe("Enterprise");
        SqlConnectionExtensions.ServerEditionIDs.ForwardIndex[-2117995310L].ShouldBe("Developer");
        SqlConnectionExtensions.ServerEditionIDs.ForwardIndex[-1534726760L].ShouldBe("Standard");
    }

    [Fact]
    public void ServerEditionIDs_HasAnEntryForEveryEnumMember() =>
        SqlConnectionExtensions.ServerEditionIDs.ForwardIndex.Count.ShouldBe(Enum.GetValues(typeof(ServerEditionID)).Length);

    [Fact]
    public void ServerEngineEditions_FallsBackToTheMemberNameWithoutADescription()
    {
        SqlConnectionExtensions.ServerEngineEditions.ForwardIndex[3].ShouldBe("Enterprise");
        SqlConnectionExtensions.ServerEngineEditions.ForwardIndex[7].ShouldBe(nameof(ServerEngineEdition.SqlStretchDatabase));
        SqlConnectionExtensions.ServerEngineEditions.ForwardIndex[11].ShouldBe(nameof(ServerEngineEdition.SqlOnDemand));
    }

    [Fact]
    public void PropsSql_DeclaresTheTableAndSelectsEveryServerProperty()
    {
        var sql = PropsSql;

        sql.ShouldStartWith("DECLARE @props TABLE (propertyname sysname PRIMARY KEY)");
        sql.ShouldContain("INSERT INTO @props(propertyname)");
        sql.ShouldContain("SELECT propertyname, SERVERPROPERTY(propertyname) AS propertyvalue FROM @props");

        foreach (var name in SqlConnectionExtensions.ServerProps.ForwardIndex.Keys)
            sql.ShouldContain($"SELECT '{name}'");
    }

    [Fact]
    public void PropsSql_JoinsAllButTheLastSelectWithUnion()
    {
        var lines = PropsSql.Split(["\r\n", "\n"], StringSplitOptions.None);
        var selectCount = lines.Count(x => x.StartsWith("SELECT '", StringComparison.Ordinal));

        lines.Count(x => x == "UNION").ShouldBe(selectCount - 1);
        selectCount.ShouldBe(SqlConnectionExtensions.ServerProps.ForwardIndex.Count);
    }

    [Fact]
    public void RunSql_ThrowsWithoutContactingAServer_WhenTheConnectionStringIsEmpty()
    {
        using var connection = new SqlConnection();

        Should.Throw<Exception>(() => connection.RunSql("SELECT 1"));
    }

    [Fact]
    public async Task RunSqlAsync_ThrowsWithoutContactingAServer_WhenTheConnectionStringIsEmpty()
    {
        using var connection = new SqlConnection();

        await Should.ThrowAsync<Exception>(() => connection.RunSqlAsync("SELECT 1"));
    }

    [Fact]
    public async Task LoadDatabasesAsync_Throws_WhenTheConnectionCannotBeOpened()
    {
        using var connection = new SqlConnection();

        await Should.ThrowAsync<Exception>(connection.LoadDatabasesAsync);
    }

    [Fact]
    public async Task LoadTablesAsync_Throws_WhenTheConnectionCannotBeOpened()
    {
        using var connection = new SqlConnection();

        await Should.ThrowAsync<Exception>(connection.LoadTablesAsync);
    }

    [Fact]
    public async Task LoadColumnsAsync_DoesNotReportProgress_WhenTheQueryFails()
    {
        using var connection = new SqlConnection();
        var reported = false;
        var progress = new Progress<bool>(_ => reported = true);

        await Should.ThrowAsync<Exception>(() => connection.LoadColumnsAsync(42, progress));

        reported.ShouldBeFalse();
    }

    [Fact]
    public void EveryPublicExtensionMethod_TakesTheMicrosoftSqlConnection()
    {
        var methods = typeof(SqlConnectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
            .ToArray();

        methods.ShouldNotBeEmpty();
        methods.ShouldAllBe(x => x.GetParameters()[0].ParameterType == typeof(SqlConnection));
    }

    [Fact]
    public void Sqlx_DoesNotReferenceTheDeprecatedSystemDataSqlClient() =>
        typeof(SqlConnectionExtensions).Assembly.GetReferencedAssemblies()
            .ShouldNotContain(x => x.Name == "System.Data.SqlClient");
}
