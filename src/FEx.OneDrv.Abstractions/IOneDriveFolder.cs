namespace FEx.OneDrv.Abstractions;

/// <summary>A folder stored in OneDrive.</summary>
public interface IOneDriveFolder
{
    string Id { get; }
    string Name { get; }
    string? Path { get; }
    int? ChildCount { get; }
}