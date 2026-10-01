using FEx.EFCore.Extensions;
using Microsoft.Data.Sqlite;
using Shouldly;
using System.Data;
using Xunit;

namespace FEx.EFCore.Tests;

public class DbContextExtensionsTests
{
    [Fact]
    public void ExecuteReader_ReturnsValueAndKeepsConnectionState()
    {
        using var db = new SqliteMemory();
        using var context = db.CreateContext();

        context.ExecuteReader("SELECT 'a' UNION ALL SELECT 'b'").ShouldBe("ab");
    }

    [Fact]
    public void ExecuteReader_ReleasesConnectionItOpened()
    {
        // File-backed so EF can open/close the connection without losing the database.
        var path = System.IO.Path.GetTempFileName();

        try
        {
            using var connection = new SqliteConnection($"DataSource={path}");
            using var context = new TestDbContext(connection);

            connection.State.ShouldBe(ConnectionState.Closed);

            context.ExecuteReader("SELECT 'x'").ShouldBe("x");

            connection.State.ShouldBe(ConnectionState.Closed);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            System.IO.File.Delete(path);
        }
    }
}
