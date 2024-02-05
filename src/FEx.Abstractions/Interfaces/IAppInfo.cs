using System;

namespace FEx.Abstractions.Interfaces;

public interface IAppInfo
{
    string Name { get; }
    Version Version { get; }
    string Company { get; }
    bool IsUIApp { get; }
}