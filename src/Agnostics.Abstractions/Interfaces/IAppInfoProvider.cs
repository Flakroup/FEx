using System;
using System.IO;
using System.Reflection;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>Provides identity, version and storage-location information about the running application.</summary>
public interface IAppInfoProvider
{
    /// <summary>Gets the simple name of the entry assembly.</summary>
    string EntryAssemblyName { get; }
    /// <summary>Gets the entry assembly of the application.</summary>
    Assembly EntryAssembly { get; }
    /// <summary>Gets the file location of the entry assembly.</summary>
    FileInfo EntryAssemblyLocation { get; }
    /// <summary>Gets the application name.</summary>
    string Name { get; }
    /// <summary>Gets the application version.</summary>
    Version Version { get; }
    /// <summary>Gets the application version as text.</summary>
    string VersionString { get; }
    /// <summary>Gets the company that publishes the application.</summary>
    string Company { get; }
    /// <summary>Gets the application copyright notice.</summary>
    string Copyright { get; }
    /// <summary>Gets the application name followed by its version, with a version prefix.</summary>
    string NameAndVersionWithPrefix { get; }
    /// <summary>Gets the application name and its version on separate lines.</summary>
    string NameLineVersion { get; }
    /// <summary>Gets the application name and its version on separate lines, with a version prefix.</summary>
    public string NameLineVersionWithPrefix { get; }
    /// <summary>Gets the application name followed by its version.</summary>
    string NameAndVersion { get; }
    /// <summary>Gets the per-user data directory of the application.</summary>
    DirectoryInfo UserData { get; }
    /// <summary>Gets the path of the per-user data directory.</summary>
    string UserDataPath { get; }
    /// <summary>Gets the machine-wide application data directory.</summary>
    DirectoryInfo AppData { get; }
    /// <summary>Gets the path of the machine-wide application data directory.</summary>
    string AppDataPath { get; }
    /// <summary>Gets the path of the per-user settings file.</summary>
    string UserSettingsPath { get; }
    /// <summary>Gets the underlying application information.</summary>
    IAppInfo AppInfo { get; }
}