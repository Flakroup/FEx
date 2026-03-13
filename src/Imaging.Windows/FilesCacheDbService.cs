using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Services;
using FEx.Imaging.Windows.Model;
using FEx.Sqlx.Abstractions;

namespace FEx.Imaging.Windows;

public class FilesCacheDbService : EFCoreDatabaseBackedService<FilesCacheContext>
{
    public FilesCacheDbService(IScopeProvider scopeProvider,
                               IFilesCacheServiceConfig config,
                               ResilientTransaction resilientTransaction,
                               ISqlDbHelper dbHelper,
                               IAsyncInitializable[] dependencies)
        : base(scopeProvider, config.DbServiceConfig, resilientTransaction, dbHelper, dependencies)
    {
        BeginInitialization();
    }
}
