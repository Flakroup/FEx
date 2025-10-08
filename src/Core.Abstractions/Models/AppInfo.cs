using FEx.Core.Abstractions.Interfaces;
using System;
using System.IO;

namespace FEx.Core.Abstractions.Models;

public record AppInfo : IAppInfo
{
    public string Name { get; init; }
    public Version Version { get; init; }
    public string Company { get; init; }
    public bool IsUIApp { get; set; }
    public DirectoryInfo UserData { get; init; }
    public DirectoryInfo AppData { get; init; }
    public string LogDirPath { get; init; }
}