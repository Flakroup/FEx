using FEx.Agnostics.Abstractions.Extensions;
using FEx.PersistentStorage.Abstractions;
using System;
using System.IO;

namespace FEx.PersistentStorage;

public sealed class DefaultDatabaseFilePathResolver : IDatabaseFilePathResolver
{
    private readonly string _appName;

    public DefaultDatabaseFilePathResolver(string appName)
    {
        _appName = appName.Guard(nameof(appName));
    }

    public string GetDatabasesFolderPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(appData, _appName, "Data");
    }
}