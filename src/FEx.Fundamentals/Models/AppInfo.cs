using FEx.Abstractions.Interfaces;
using System;
using System.IO;

namespace FEx.Fundamentals.Models;

public record AppInfo : IAppInfo
{
    public string Name { get; init; }
    public Version Version { get; init; }
    public string Company { get; init; }
    public bool IsUIApp { get; init; }
    public DirectoryInfo UserData { get; init; }
    public DirectoryInfo AppData { get; init; }
    public string LogDirPath { get; init; }
}