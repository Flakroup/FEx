using FEx.Sqlx.Enums;
using FEx.Sqlx.Extensions;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MsSqlConnection = Microsoft.Data.SqlClient.SqlConnection;

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
        using var connection = new MsSqlConnection();

        Should.Throw<Exception>(() => connection.RunSql("SELECT 1"));
    }

    [Fact]
    public async Task RunSqlAsync_ThrowsWithoutContactingAServer_WhenTheConnectionStringIsEmpty()
    {
        using var connection = new MsSqlConnection();

        await Should.ThrowAsync<Exception>(() => connection.RunSqlAsync("SELECT 1"));
    }
}
