using System;
using System.IO;
using System.Reflection;

namespace FEx.Abstractions;

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
    string NameAndVersion { get; }
    DirectoryInfo UserData { get; }
    string UserDataPath { get; }
    DirectoryInfo AppData { get; }
    string AppDataPath { get; }
    string UserSettingsPath { get; }
    string LogFilePath { get; }
}