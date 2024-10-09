using System;
using System.IO;

namespace FEx.Common.Abstractions.Interfaces;

public interface IAppInfo
{
    string Name { get; }
    Version Version { get; }
    string Company { get; }
    bool IsUIApp { get; set; }
    DirectoryInfo UserData { get; }
    DirectoryInfo AppData { get; }
    string LogDirPath { get; }
}