using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

namespace FEx.EFCore.Tests;

public sealed class TestDbContext : DbContext
{
    public TestDbContext(SqliteConnection connection)
        : base(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options)
    {
    }
}

public sealed class SqliteMemory : IDisposable
{
    public SqliteConnection Connection { get; } = new("DataSource=:memory:");

    public SqliteMemory() => Connection.Open();

    public TestDbContext CreateContext() => new(Connection);

    public void Dispose() => Connection.Dispose();
}
