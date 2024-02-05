using FEx.Abstractions.Interfaces;
using System;

namespace FEx.Fundamentals.Models;

public record AppInfo : IAppInfo
{
    public string Name { get; init; }
    public Version Version { get; init; }
    public string Company { get; init; }
    public bool IsUIApp { get; init; }
}