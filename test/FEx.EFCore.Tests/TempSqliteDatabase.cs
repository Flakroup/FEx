using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace FEx.EFCore.Tests;

/// <summary>
/// One file-backed SQLite database in the temp directory. Production gives every context its own connection to a
/// file, so a fixture does the same: a context built on <see cref="ConnectionString" /> opens and owns its connection.
/// One shared in-memory connection lets overlapping contexts open transactions on the same connection and lets a late
/// background save reopen a disposed connection as an empty database.
/// </summary>
public sealed class TempSqliteDatabase : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"fex-efcore-tests-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={_path}";

    public void Dispose()
    {
        // The pool keeps the file open after a connection is disposed, which blocks the delete on Windows.
        SqliteConnection.ClearAllPools();

        try
        {
            File.Delete(_path);
        }
        catch (IOException)
        {
            // still held for a moment; the file lives under the temp path anyway
        }
        catch (UnauthorizedAccessException)
        {
            // same as above
        }
    }
}
