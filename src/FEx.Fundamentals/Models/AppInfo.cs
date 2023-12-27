using System;

namespace FEx.Fundamentals.Models;

public record AppInfo
{
    public string Name { get; init; }
    public Version Version { get; init; }
}