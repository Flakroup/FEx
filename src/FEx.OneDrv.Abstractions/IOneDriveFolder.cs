namespace FEx.OneDrv.Abstractions;

public interface IOneDriveFolder
{
    string Id { get; }
    string Name { get; }
    string Path { get; }
    int? ChildCount { get; }
}
