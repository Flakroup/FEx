using FEx.Abstractions.Interfaces;
using FEx.LiteDBx.Abstractions.Interfaces;
using System.IO;

namespace FEx.LiteDBx;

public class DatabaseFilePathResolver : IDatabaseFilePathResolver
{
    private readonly IAppInfoProvider _appInfoProvider;

    public string AppDataDirectory => _appInfoProvider.AppData.FullName;

    public DatabaseFilePathResolver(IAppInfoProvider appInfoProvider)
    {
        _appInfoProvider = appInfoProvider;
    }

    public string GetDatabasesFolderPath() => Directory.CreateDirectory(Path.Combine(AppDataDirectory, ".db")).FullName;
}