using FEx.OneDrv.Abstractions;

namespace FEx.OneDrv.Models;

internal sealed class OneDriveFolder : IOneDriveFolder
{
    // Always populated by DriveItemMapper (guarded non-null there).
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Path { get; init; }
    public int? ChildCount { get; init; }
}