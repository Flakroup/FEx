using FEx.Sqlx.Extensions;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Sqlx.Tests;

/// <summary>
/// The overloads for the obsolete <c>System.Data.SqlClient</c> connection, kept for backward compatibility; the class is
/// itself obsolete so that using the obsolete type here does not raise CS0618.
/// </summary>
[Obsolete("Exercises the retained System.Data.SqlClient overloads.")]
public sealed class LegacySqlConnectionExtensionsTests
{
    [Fact]
    public void RunSql_LegacyConnection_ThrowsWithoutContactingAServer_WhenTheConnectionStringIsEmpty()
    {
        using var connection = new System.Data.SqlClient.SqlConnection();

        Should.Throw<Exception>(() => connection.RunSql("SELECT 1"));
    }

    [Fact]
    public async Task RunSqlAsync_LegacyConnection_ThrowsWithoutContactingAServer_WhenTheConnectionStringIsEmpty()
    {
        using var connection = new System.Data.SqlClient.SqlConnection();

        await Should.ThrowAsync<Exception>(() => connection.RunSqlAsync("SELECT 1"));
    }

    [Fact]
    public async Task LoadDatabasesAsync_Throws_WhenTheConnectionCannotBeOpened()
    {
        using var connection = new System.Data.SqlClient.SqlConnection();

        await Should.ThrowAsync<Exception>(connection.LoadDatabasesAsync);
    }

    [Fact]
    public async Task LoadTablesAsync_Throws_WhenTheConnectionCannotBeOpened()
    {
        using var connection = new System.Data.SqlClient.SqlConnection();

        await Should.ThrowAsync<Exception>(connection.LoadTablesAsync);
    }

    [Fact]
    public async Task LoadColumnsAsync_DoesNotReportProgress_WhenTheQueryFails()
    {
        using var connection = new System.Data.SqlClient.SqlConnection();
        var reported = false;
        var progress = new Progress<bool>(_ => reported = true);

        await Should.ThrowAsync<Exception>(() => connection.LoadColumnsAsync(42, progress));

        reported.ShouldBeFalse();
    }
}
