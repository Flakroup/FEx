using System;
using System.IO;

namespace FEx.Abstractions.Interfaces;

public interface IAppInfo
{
    string Name { get; }
    Version Version { get; }
    string Company { get; }
    bool IsUIApp { get; }
    DirectoryInfo UserData { get; }
    DirectoryInfo AppData { get; }
    string LogDirPath { get; }
}