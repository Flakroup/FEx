using FEx.Agnostics.Utilities;
using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Extensions;
using LiteDB;
using System;
using System.IO;

namespace FEx.PersistentStorage;

public sealed class DatabaseProvider : IDatabaseProvider, IDisposable
{
    private bool _isDisposed;

    public ILiteRepository Repository { get; }
    public ExtendedReaderWriterLockSlim DbLock { get; }

    public DatabaseProvider(IDatabaseFilePathResolver filePathResolver)
    {
        Repository = GetRepository(filePathResolver);
        DbLock = new(this);
    }

    private static LiteRepository GetRepository(IDatabaseFilePathResolver filePathResolver)
    {
        //we need to detect whether it is already registered 
        const string dbFileName = "localLiteDb.db";

        var databaseDirectoryPath = filePathResolver.GetDatabasesFolderPath();
        var dbFilePath = Path.Combine(databaseDirectoryPath, dbFileName);

        return LiteRepositoryExtensions.GetRepository(dbFilePath);
    }

    #region IDisposable
    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            Repository.Dispose();

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}