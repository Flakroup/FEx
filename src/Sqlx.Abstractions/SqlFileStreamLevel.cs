namespace FEx.Sqlx.Abstractions;

/// <summary>SMO-free mirror of the FILESTREAM access level.</summary>
public enum SqlFileStreamLevel
{
    Disabled = 0,
    SqlAccess = 1,
    SqlLocalFileSystemAccess = 2,
    SqlFullFileSystemAccess = 3
}
