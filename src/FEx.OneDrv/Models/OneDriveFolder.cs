using FEx.OneDrv.Abstractions;

namespace FEx.OneDrv.Models;

internal sealed class OneDriveFolder : IOneDriveFolder
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Path { get; init; }
    public int? ChildCount { get; init; }
}