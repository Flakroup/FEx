namespace FEx.Sqlx.Abstractions;

/// <summary>SMO-free mirror of the FILESTREAM access level.</summary>
public enum SqlFileStreamLevel
{
    Disabled = 0,
    SqlAccess = 1,
    SqlLocalFileSystemAccess = 2,
    SqlFullFileSystemAccess = 3,

    /// <summary>The server reported a level this enum does not know.</summary>
    Unknown = -1
}
