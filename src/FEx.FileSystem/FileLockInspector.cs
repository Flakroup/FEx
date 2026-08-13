using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace FEx.FileSystem;

/// <summary>
/// Detects which Windows process(es) hold a lock on one or more files using the Restart Manager
/// API (rstrtmgr.dll). Returns an empty list on non-Windows platforms or when no locker can be
/// determined - this method never throws.
/// </summary>
public static class FileLockInspector
{
    // ReSharper disable InconsistentNaming
    private const int RmRebootReasonNone = 0;
    private const int CCH_RM_MAX_APP_NAME = 255;
    private const int CCH_RM_MAX_SVC_NAME = 63;
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_MORE_DATA = 234;
    // ReSharper restore InconsistentNaming

    /// <summary>
    /// Finds the process(es) locking a single file. See <see cref="WhoIsLocking(IReadOnlyCollection{string})" />.
    /// </summary>
    public static IReadOnlyList<LockingProcessInfo> WhoIsLocking(string? path) =>
        // IsNullOrWhiteSpace(path) == false guarantees non-null; ns2.0 lacks the NotNullWhen annotation.
        WhoIsLocking(string.IsNullOrWhiteSpace(path) ? [] : [path!]);

    /// <summary>
    /// Finds the distinct process(es) locking any of the supplied files. Returns an empty list on
    /// non-Windows platforms, for an empty input, or when the Restart Manager cannot determine a locker.
    /// </summary>
    public static IReadOnlyList<LockingProcessInfo> WhoIsLocking(IReadOnlyCollection<string> paths)
    {
        if (paths is null || paths.Count == 0)
            return [];

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return [];

        var resources = paths.Where(static path => !string.IsNullOrWhiteSpace(path)).Distinct().ToArray();

        return resources.Length == 0 ? [] : QueryRestartManager(resources);
    }

    private static IReadOnlyList<LockingProcessInfo> QueryRestartManager(string[] resources)
    {
        var key = Guid.NewGuid().ToString();

        if (RmStartSession(out var handle, 0, key) != ERROR_SUCCESS)
        {
            Log.Debug("Restart Manager: could not begin a session to inspect file locks");

            return [];
        }

        try
        {
            var registration = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

            if (registration != ERROR_SUCCESS)
            {
                Log.Debug("Restart Manager: could not register {Count} resource(s), code {Code}",
                    resources.Length,
                    registration);

                return [];
            }

            return ReadLockingProcesses(handle);
        }
        finally
        {
            RmEndSession(handle);
        }
    }

    private static IReadOnlyList<LockingProcessInfo> ReadLockingProcesses(uint handle)
    {
        uint processInfoCount = 0;
        uint rebootReasons = RmRebootReasonNone;

        // First call returns the required buffer size via ERROR_MORE_DATA.
        var probe = RmGetList(handle, out var needed, ref processInfoCount, null, ref rebootReasons);

        if (probe == ERROR_SUCCESS || needed == 0)
            return [];

        if (probe != ERROR_MORE_DATA)
        {
            Log.Debug("Restart Manager: could not size the locker list, code {Code}", probe);

            return [];
        }

        var processInfo = new RM_PROCESS_INFO[needed];
        processInfoCount = needed;

        var list = RmGetList(handle, out needed, ref processInfoCount, processInfo, ref rebootReasons);

        if (list != ERROR_SUCCESS)
        {
            Log.Debug("Restart Manager: could not read the locker list, code {Code}", list);

            return [];
        }

        var result = new List<LockingProcessInfo>((int)processInfoCount);

        for (var i = 0; i < processInfoCount; i++)
        {
            var info = processInfo[i];
            var name = string.IsNullOrWhiteSpace(info.strAppName) ? "(unknown)" : info.strAppName;
            result.Add(new(info.Process.dwProcessId, name, info.ApplicationType));
        }

        return result;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint pSessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(uint pSessionHandle,
                                                   uint nFiles,
                                                   string[] rgsFilenames,
                                                   uint nApplications,
                                                   [In] RM_UNIQUE_PROCESS[]? rgApplications,
                                                   uint nServices,
                                                   string[]? rgsServiceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(uint dwSessionHandle,
                                        out uint pnProcInfoNeeded,
                                        ref uint pnProcInfo,
                                        [In] [Out] RM_PROCESS_INFO[]? rgAffectedApps,
                                        ref uint lpdwRebootReasons);

    // ReSharper disable InconsistentNaming
    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS
    {
        public readonly int dwProcessId;
        public readonly FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RM_PROCESS_INFO
    {
        public readonly RM_UNIQUE_PROCESS Process;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
        public readonly string strAppName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
        public readonly string strServiceShortName;

        public readonly LockingProcessType ApplicationType;
        public readonly uint AppStatus;
        public readonly uint TSSessionId;

        [MarshalAs(UnmanagedType.Bool)]
        public readonly bool bRestartable;
    }
    // ReSharper restore InconsistentNaming
}
