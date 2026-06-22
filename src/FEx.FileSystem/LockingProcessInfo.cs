namespace FEx.FileSystem;

/// <summary>
/// Describes a process that holds a lock on a file, as reported by the Windows Restart Manager.
/// </summary>
public readonly struct LockingProcessInfo
{
    public LockingProcessInfo(int processId, string processName, LockingProcessType type)
    {
        ProcessId = processId;
        ProcessName = processName;
        Type = type;
    }

    public int ProcessId { get; }

    public string ProcessName { get; }

    public LockingProcessType Type { get; }
}

/// <summary>
/// Mirrors the Restart Manager RM_APP_TYPE values describing how a locking process is hosted.
/// </summary>
public enum LockingProcessType
{
    Unknown = 0,
    MainWindow = 1,
    OtherWindow = 2,
    Service = 3,
    Explorer = 4,
    Console = 5,
    Critical = 1000
}
