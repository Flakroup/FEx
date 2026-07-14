using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.IO;

namespace FEx.Core.Abstractions.Models;

public record AppInfo : IAppInfo
{
    // Non-null-annotated per IAppInfo; expected to be supplied via object initializer at construction.
    public string Name { get; init; } = null!;
    public Version Version { get; init; } = null!;
    public string Company { get; init; } = null!;
    public bool IsUIApp { get; set; }
    public DirectoryInfo UserData { get; init; } = null!;
    public DirectoryInfo AppData { get; init; } = null!;
    public string LogDirPath { get; init; } = null!;
}