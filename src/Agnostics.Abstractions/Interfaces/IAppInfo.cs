using System;
using System.IO;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>Describes the running application and where it stores its data and logs.</summary>
public interface IAppInfo
{
    /// <summary>Gets the application name.</summary>
    string Name { get; }
    /// <summary>Gets the application version.</summary>
    Version Version { get; }
    /// <summary>Gets the company that publishes the application.</summary>
    string Company { get; }
    /// <summary>Gets or sets a value indicating whether the application has a user interface.</summary>
    bool IsUIApp { get; set; }
    /// <summary>Gets the per-user data directory.</summary>
    DirectoryInfo UserData { get; }
    /// <summary>Gets the machine-wide application data directory.</summary>
    DirectoryInfo AppData { get; }
    /// <summary>Gets the path of the directory where log files are written.</summary>
    string LogDirPath { get; }
}