using System.Diagnostics;
using System.Linq;

namespace FEx.Common.Utilities;

public static class AppUtility
{
    public static bool IsSingleInstance() => GetOtherInstances().Length == 0;

    public static int[] GetOtherInstances()
    {
        var currentProcess = Process.GetCurrentProcess();

        return [.. Process.GetProcesses()
            .Where(x => x.ProcessName == currentProcess.ProcessName && x.Id != currentProcess.Id && x.Threads.Count > 0)
            .Select(x => x.Id)];
    }
}