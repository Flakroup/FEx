using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace FEx.WPFx.Natives;

public static class FileUtil
{
    private const int RmRebootReasonNone = 0;

    /// <summary>
    /// Find out what process(es) have a lock on the specified file.
    /// </summary>
    /// <param name="path">Path of the file.</param>
    /// <returns>Processes locking the file</returns>
    /// <remarks>
    /// See also:
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
    /// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)
    /// </remarks>
    public static List<Process> WhoIsLocking(string path)
    {
        var key = Guid.NewGuid().ToString();
        var processes = new List<Process>();

        var res = RmStartSession(out var handle, 0, key);

        if (res != 0)
            throw new("Could not begin restart session.  Unable to determine file locker.");

        try
        {
            // ReSharper disable InconsistentNaming
            const int ERROR_MORE_DATA = 234;
            // ReSharper restore InconsistentNaming
            uint pnProcInfo = 0, lpdwRebootReasons = RmRebootReasonNone;

            string[] resources = [path]; // Just checking on one resource.

            res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

            if (res != 0)
                throw new("Could not register resource.");

            //Note: there's a race condition here -- the first call to RmGetList() returns
            //      the total number of process. However, when we call RmGetList() again to get
            //      the actual processes this number may have increased.
            res = RmGetList(handle, out var pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

            if (res == ERROR_MORE_DATA)
            {
                // Create an array to store the process results
                var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                pnProcInfo = pnProcInfoNeeded;

                // Get the list
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);

                if (res == 0)
                {
                    processes = new((int)pnProcInfo);

                    // Enumerate all of the results and add them to the 
                    // list to be returned
                    for (var i = 0; i < pnProcInfo; i++)
                    {
                        try
                        {
                            processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                        }
                        // catch the error -- in case the process is no longer running
                        catch (ArgumentException)
                        {
                        }
                    }
                }
                else
                {
                    throw new("Could not list processes locking resource.");
                }
            }
            else if (res != 0)
            {
                throw new("Could not list processes locking resource. Failed to get size of result.");
            }
        }
        finally
        {
            RmEndSession(handle);
        }

        return processes;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(uint pSessionHandle,
                                                  uint nFiles,
                                                  string[] rgsFilenames,
                                                  uint nApplications,
                                                  [In] RM_UNIQUE_PROCESS[] rgApplications,
                                                  uint nServices,
                                                  string[] rgsServiceNames);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint pSessionHandle);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(uint dwSessionHandle,
                                        out uint pnProcInfoNeeded,
                                        ref uint pnProcInfo,
                                        [In] [Out] RM_PROCESS_INFO[] rgAffectedApps,
                                        ref uint lpdwRebootReasons);

    // ReSharper disable InconsistentNaming
    internal enum RM_APP_TYPE
        // ReSharper restore InconsistentNaming
    {
        RmUnknownApp = 0,
        RmMainWindow = 1,
        RmOtherWindow = 2,
        RmService = 3,
        RmExplorer = 4,
        RmConsole = 5,
        RmCritical = 1000
    }

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable InconsistentNaming
    private struct RM_UNIQUE_PROCESS
        // ReSharper restore InconsistentNaming
    {
        public readonly int dwProcessId;
        public readonly FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    // ReSharper disable InconsistentNaming
    private struct RM_PROCESS_INFO
        // ReSharper restore InconsistentNaming
    {
        public readonly RM_UNIQUE_PROCESS Process;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
        public readonly string strAppName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
        public readonly string strServiceShortName;

        public readonly RM_APP_TYPE ApplicationType;
        public readonly uint AppStatus;
        public readonly uint TSSessionId;

        [MarshalAs(UnmanagedType.Bool)]
        public readonly bool bRestartable;
    }

    // ReSharper disable InconsistentNaming
    private const int CCH_RM_MAX_APP_NAME = 255;

    private const int CCH_RM_MAX_SVC_NAME = 63;
    // ReSharper restore InconsistentNaming
}