namespace FEx.Sqlx.Abstractions;

/// <summary>SMO-free mirror of the Always On availability groups manager status.</summary>
public enum SqlHadrManagerStatus
{
    PendingCommunication = 0,
    Running = 1,
    Failed = 2,

    /// <summary>The server reported a status this enum does not know.</summary>
    Unknown = -1
}
