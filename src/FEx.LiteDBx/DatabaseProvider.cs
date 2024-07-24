using FEx.Basics.Utilities;
using FEx.LiteDBx.Abstractions.Interfaces;
using LiteDB;
using System;
using System.IO;

namespace FEx.LiteDBx;

public sealed class DatabaseProvider : IDatabaseProvider, IDisposable
{
    private bool _isDisposed;

    public ILiteRepository Repository { get; }
    public ExtendedReaderWriterLockSlim DbLock { get; }

    public DatabaseProvider(IDatabaseFilePathResolver filePathResolver)
    {
        Repository = GetRepository(filePathResolver);
        DbLock = new();
    }

    private static LiteRepository GetRepository(IDatabaseFilePathResolver filePathResolver)
    {
        const string dbFileName = "localLiteDb.db";

        string databaseDirectoryPath = filePathResolver.GetDatabasesFolderPath();
        string dbFilePath = Path.Combine(databaseDirectoryPath, dbFileName);

        return LiteRepositoryFactory.GetRepository(dbFilePath);
    }

    #region IDisposable
    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            Repository.Dispose();
            DbLock.Dispose();
        }

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}