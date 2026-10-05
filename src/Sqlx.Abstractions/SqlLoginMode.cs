namespace FEx.Sqlx.Abstractions;

/// <summary>SMO-free mirror of the SQL Server authentication mode.</summary>
public enum SqlLoginMode
{
    Normal = 0,
    Integrated = 1,
    Mixed = 2,
    Unknown = 9
}
