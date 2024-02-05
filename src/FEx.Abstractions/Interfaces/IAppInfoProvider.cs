using System;
using System.IO;
using System.Reflection;

namespace FEx.Abstractions.Interfaces;

public interface IAppInfoProvider
{
    string EntryAssemblyName { get; }
    Assembly EntryAssembly { get; }
    FileInfo EntryAssemblyLocation { get; }
    string Name { get; }
    Version Version { get; }
    string VersionString { get; }
    string Company { get; }
    string Copyright { get; }
    string NameAndVersionWithPrefix { get; }
    string NameLineVersion { get; }
    public string NameLineVersionWithPrefix { get; }
    string NameAndVersion { get; }
    DirectoryInfo UserData { get; }
    string UserDataPath { get; }
    DirectoryInfo AppData { get; }
    string AppDataPath { get; }
    string UserSettingsPath { get; }
    string LogFilePath { get; }
}