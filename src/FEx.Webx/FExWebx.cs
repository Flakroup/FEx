using System.Net;

namespace FEx.Webx;

public class FExWebx
{
    // SYSLIB0014: ServicePointManager is obsolete on net5+ (no-op for HttpClient); this thin
    // wrapper intentionally exposes it for netstandard/.NET Framework consumers where it still
    // governs the process-wide connection limit.
#pragma warning disable SYSLIB0014
    public static int DefaultConnectionLimit
    {
        get => ServicePointManager.DefaultConnectionLimit;
        set => ServicePointManager.DefaultConnectionLimit = value;
    }
#pragma warning restore SYSLIB0014
}