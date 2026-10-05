using Shouldly;
using System.Data;
using System.Reflection;
using Xunit;

namespace FEx.Sqlx.Tests;

/// <summary>The pure helpers of <see cref="SqlDbHelper" />; instance discovery itself needs a machine with SQL Server.</summary>
public sealed class SqlDbHelperTests
{
    [Fact]
    public void ServiceNameToInstanceName_MapsTheDefaultInstanceToTheMachineName() =>
        ServiceNameToInstanceName("HOST", "MSSQLSERVER").ShouldBe("HOST");

    [Fact]
    public void ServiceNameToInstanceName_IsCaseInsensitiveForTheDefaultInstance() =>
        ServiceNameToInstanceName("HOST", "mssqlserver").ShouldBe("HOST");

    [Fact]
    public void ServiceNameToInstanceName_MapsANamedInstanceToMachineBackslashName() =>
        ServiceNameToInstanceName("HOST", "MSSQL$SQL2022").ShouldBe(@"HOST\SQL2022");

    [Fact]
    public void ServiceNameToInstanceName_IgnoresThePrefixCase() =>
        ServiceNameToInstanceName("HOST", "mssql$Dev").ShouldBe(@"HOST\Dev");

    [Theory]
    [InlineData("SQLAgent$SQL2022")]
    [InlineData("SomethingElse")]
    [InlineData("")]
    [InlineData(null)]
    public void ServiceNameToInstanceName_ReturnsNull_ForServicesThatAreNotDatabaseEngines(string? serviceName) =>
        ServiceNameToInstanceName("HOST", serviceName).ShouldBeNull();

    [Fact]
    public void GetSqlInstanceName_ReturnsTheServerName_WhenThereIsNoInstanceName() =>
        GetSqlInstanceName("HOST", null).ShouldBe("HOST");

    [Fact]
    public void GetSqlInstanceName_ReturnsTheServerName_WhenTheInstanceNameIsEmpty() =>
        GetSqlInstanceName("HOST", string.Empty).ShouldBe("HOST");

    [Fact]
    public void GetSqlInstanceName_JoinsServerAndInstanceWithABackslash() =>
        GetSqlInstanceName("HOST", "SQL2022").ShouldBe(@"HOST\SQL2022");

    [Fact]
    public void HasValidLoginMode_RejectsAnInstanceWhoseLoginModeIsUnknown() =>
        Invoke<bool>("HasValidLoginMode", new SQLInstanceInfo("HOST")).ShouldBeFalse();

    [Fact]
    public void HasValidLoginMode_RejectsAMissingInstance() =>
        Invoke<bool>("HasValidLoginMode", [null]).ShouldBeFalse();

    private static string? ServiceNameToInstanceName(string machineName, string? serviceName) =>
        Invoke<string?>("ServiceNameToInstanceName", machineName, serviceName);

    private static string GetSqlInstanceName(string serverName, string? instanceName)
    {
        using var table = new DataTable();
        table.Columns.AddRange([new DataColumn("ServerName", typeof(string)), new DataColumn("InstanceName", typeof(string))]);
        table.Rows.Add(serverName, instanceName);

        return Invoke<string>("GetSqlInstanceName", table.Rows[0]);
    }

    private static T Invoke<T>(string methodName, params object?[] args) =>
        (T)typeof(SqlDbHelper)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, args)!;
}
