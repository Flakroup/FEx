using Microsoft.Data.Sqlite;
using Shouldly;
using System.IO;
using Xunit;

namespace FEx.EFCore.Tests;

public sealed class TempSqliteDatabaseTests
{
    [Fact]
    public void Dispose_DeletesTheDatabaseFile_EvenAfterAConnectionWasPooled()
    {
        string path;

        using (var sut = new TempSqliteDatabase())
        {
            path = sut.ConnectionString["Data Source=".Length..];

            using var connection = new SqliteConnection(sut.ConnectionString);
            connection.Open();
            File.Exists(path).ShouldBeTrue();
        }

        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public void EachInstance_HasItsOwnDatabase()
    {
        using var first = new TempSqliteDatabase();
        using var second = new TempSqliteDatabase();

        first.ConnectionString.ShouldNotBe(second.ConnectionString);
    }
}
