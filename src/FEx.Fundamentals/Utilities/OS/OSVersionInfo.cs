using FEx.Extensions;
using FEx.Extensions.Collections.Dictionaries;
using FEx.Fundamentals.Utilities.OS.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

// http://www.codeproject.com/Articles/73000/Getting-Operating-System-Version-Info-Even-for-Win
//https://en.wikipedia.org/wiki/List_of_Microsoft_Windows_versions

//Thanks to Member 7861383, Scott Vickery for the Windows 8.1 update and workaround.
//I have moved it to the beginning of the Name property, though...

//Thanks to Brisingr Aerowing for help with the Windows 10 adaptation

namespace FEx.Fundamentals.Utilities.OS;

/// <summary>
/// Provides detailed information about the host operating system.
/// </summary>
public static class OSVersionInfo
{
    private const int SmTabletPC = 86;

    /// <summary>
    /// Indicates whether the operating-system is arm64.
    /// </summary>
    public static bool IsArm64 => RuntimeInformation.OSArchitecture == Architecture.Arm64;

    /// <summary>
    /// Indicates whether the operating-system is 64bit.
    /// </summary>
    public static bool Is64Bit =>
        RuntimeInformation.OSArchitecture is Architecture.X64
            or Architecture.Arm64;

    /// <summary>
    /// Indicates whether the operating-system is 32bit.
    /// </summary>
    public static bool Is32Bit => !Is64Bit;

    /// <summary>
    /// Indicates whether the operating-system is UNIX.
    /// </summary>
    public static bool IsUnix => IsLinux || IsMacOS || IsIOS;

    /// <summary>
    /// Indicates whether the current process is running under Windows Subsystem for Linux.
    /// </summary>
    public static bool IsWsl
    {
        get
        {
            if (!IsLinux)
                return false;

            try
            {
                string version = File.ReadAllText("/proc/version");

                return version.ContainsOrdinalIgnoreCase("Microsoft");
            }
            catch (IOException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Indicates the target framework of the current process.
    /// </summary>
    public static FrameworkName Framework => new(Assembly.GetEntryAssembly()
        .Guard()
        .GetCustomAttribute<TargetFrameworkAttribute>()
        .Guard()
        .FrameworkName);

    /// <summary>
    /// Indicates the operating-system platform.
    /// </summary>
    public static OSPlatformInfo OSPlatform =>
        IsBrowser ? OSPlatformInfo.Browser :
        IsIOS ? OSPlatformInfo.IOS :
        IsAndroid ? OSPlatformInfo.Android :
        IsMacOS ? OSPlatformInfo.OSX :
        IsLinux ? OSPlatformInfo.Linux :
        IsWindows ? OSPlatformInfo.Windows : OSPlatformInfo.Unknown;

#if NET6_0_OR_GREATER
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsMacOS => OperatingSystem.IsMacOS();
    public static bool IsLinux => OperatingSystem.IsLinux();
    public static bool IsAndroid => OperatingSystem.IsAndroid();
    public static bool IsIOS => OperatingSystem.IsIOS();
    public static bool IsBrowser => OperatingSystem.IsBrowser();
    public static bool IsOSPlatform(string platform) => OperatingSystem.IsOSPlatform(platform);
#else
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsAndroid => IsOSPlatform("ANDROID");
        public static bool IsIOS => IsOSPlatform("IOS");
        public static bool IsBrowser => IsOSPlatform("BROWSER");
        public static bool IsOSPlatform(string platform) => RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform));
#endif

    /// <summary>
    /// Determines if the current application is 32 or 64-bit.
    /// </summary>
    public static SoftwareArchitecture ProgramBits { get; }

    public static SoftwareArchitecture OSBits { get; }

    public static bool Is64BitOperatingSystem => OSBits == SoftwareArchitecture.Bit64;

    /// <summary>
    /// Determines if the current processor is 32 or 64-bit.
    /// </summary>
    public static OSProcessorArchitecture ProcessorBits { get; }

    public static OSEdition Edition { get; }

    /// <summary>
    /// Gets the edition of the operating system running on this computer.
    /// </summary>
    public static string EditionString => Edition != OSEdition.Unknown
        ? Edition.GetEnumValueDescription()
        : null;

    /// <summary>
    /// Gets the name of the operating system running on this computer.
    /// </summary>
    public static string Name { get; }

    /// <summary>
    /// Gets the service pack information of the operating system running on this computer.
    /// </summary>
    public static string ServicePack { get; }

    /// <summary>
    /// Gets the build version number of the operating system running on this computer.
    /// </summary>
    public static int BuildVersion { get; }

    /// <summary>
    /// Gets the full version of the operating system running on this computer.
    /// </summary>
    public static Version Version { get; }

    public static string InfoString { get; }

    private static Dictionary<OSProduct, OSEdition> ProductToEdition { get; } = new()
    {
        [OSProduct.Business] = OSEdition.Business,
        [OSProduct.BusinessN] = OSEdition.BusinessN,
        [OSProduct.ClusterServer] = OSEdition.HPCEdition,
        [OSProduct.ClusterServerV] = OSEdition.HPCEditionWithoutHyperV,
        [OSProduct.DatacenterServer] = OSEdition.DatacenterServer,
        [OSProduct.DatacenterServerCore] = OSEdition.DatacenterServerCoreInstallation,
        [OSProduct.DatacenterServerV] = OSEdition.DatacenterServerWithoutHyperV,
        [OSProduct.DatacenterServerCoreV] = OSEdition.DatacenterServerWithoutHyperVCoreInstallation,
        [OSProduct.Embedded] = OSEdition.Embedded,
        [OSProduct.Enterprise] = OSEdition.Enterprise,
        [OSProduct.EnterpriseN] = OSEdition.EnterpriseN,
        [OSProduct.EnterpriseE] = OSEdition.EnterpriseE,
        [OSProduct.EnterpriseServer] = OSEdition.EnterpriseServer,
        [OSProduct.EnterpriseServerCore] = OSEdition.EnterpriseServerCoreInstallation,
        [OSProduct.EnterpriseServerCoreV] = OSEdition.EnterpriseServerWithoutHyperVCoreInstallation,
        [OSProduct.EnterpriseServerIa64] = OSEdition.EnterpriseServerForItaniumBasedSystems,
        [OSProduct.EnterpriseServerV] = OSEdition.EnterpriseServerWithoutHyperV,
        [OSProduct.EssentialbusinessServerMgmt] = OSEdition.EssentialBusinessServerMGMT,
        [OSProduct.EssentialbusinessServerAddl] = OSEdition.EssentialBusinessServerADDL,
        [OSProduct.EssentialbusinessServerMgmtsvc] = OSEdition.EssentialBusinessServerMGMTSVC,
        [OSProduct.EssentialbusinessServerAddlsvc] = OSEdition.EssentialBusinessServerADDLSVC,
        [OSProduct.HomeBasic] = OSEdition.HomeBasic,
        [OSProduct.HomeBasicN] = OSEdition.HomeBasicN,
        [OSProduct.HomeBasicE] = OSEdition.HomeBasicE,
        [OSProduct.HomePremium] = OSEdition.HomePremium,
        [OSProduct.HomePremiumN] = OSEdition.HomePremiumN,
        [OSProduct.HomePremiumE] = OSEdition.HomePremiumE,
        [OSProduct.HomePremiumServer] = OSEdition.HomePremiumServer,
        [OSProduct.Hyperv] = OSEdition.MicrosoftHyperVServer,
        [OSProduct.MediumbusinessServerManagement] = OSEdition.WindowsEssentialBusinessManagementServer,
        [OSProduct.MediumbusinessServerMessaging] = OSEdition.WindowsEssentialBusinessMessagingServer,
        [OSProduct.MediumbusinessServerSecurity] = OSEdition.WindowsEssentialBusinessSecurityServer,
        [OSProduct.Professional] = OSEdition.Professional,
        [OSProduct.ProfessionalN] = OSEdition.ProfessionalN,
        [OSProduct.ProfessionalE] = OSEdition.ProfessionalE,
        [OSProduct.SbSolutionServer] = OSEdition.SBSolutionServer,
        [OSProduct.SbSolutionServerEm] = OSEdition.SBSolutionServerEM,
        [OSProduct.ServerForSbSolutions] = OSEdition.ServerForSBSolutions,
        [OSProduct.ServerForSbSolutionsEm] = OSEdition.ServerForSBSolutionsEM,
        [OSProduct.ServerForSmallbusiness] = OSEdition.WindowsEssentialServerSolutions,
        [OSProduct.ServerForSmallbusinessV] = OSEdition.WindowsEssentialServerSolutionsWithoutHyperV,
        [OSProduct.ServerFoundation] = OSEdition.ServerFoundation,
        [OSProduct.SmallbusinessServer] = OSEdition.WindowsSmallBusinessServer,
        [OSProduct.SmallbusinessServerPremium] = OSEdition.WindowsSmallBusinessServerPremium,
        [OSProduct.SmallbusinessServerPremiumCore] = OSEdition.WindowsSmallBusinessServerPremiumCoreInstallation,
        [OSProduct.SolutionEmbeddedserver] = OSEdition.SolutionEmbeddedServer,
        [OSProduct.SolutionEmbeddedserverCore] = OSEdition.SolutionEmbeddedServerCoreInstallation,
        [OSProduct.StandardServer] = OSEdition.StandardServer,
        [OSProduct.StandardServerCore] = OSEdition.StandardServerCoreInstallation,
        [OSProduct.StandardServerSolutions] = OSEdition.StandardServerSolutions,
        [OSProduct.StandardServerSolutionsCore] = OSEdition.StandardServerSolutionsCoreInstallation,
        [OSProduct.StandardServerCoreV] = OSEdition.StandardServerWithoutHyperVCoreInstallation,
        [OSProduct.StandardServerV] = OSEdition.StandardServerWithoutHyperV,
        [OSProduct.Starter] = OSEdition.Starter,
        [OSProduct.StarterN] = OSEdition.StarterN,
        [OSProduct.StarterE] = OSEdition.StarterE,
        [OSProduct.StorageEnterpriseServer] = OSEdition.EnterpriseStorageServer,
        [OSProduct.StorageEnterpriseServerCore] = OSEdition.EnterpriseStorageServerCoreInstallation,
        [OSProduct.StorageExpressServer] = OSEdition.ExpressStorageServer,
        [OSProduct.StorageExpressServerCore] = OSEdition.ExpressStorageServerCoreInstallation,
        [OSProduct.StorageStandardServer] = OSEdition.StandardStorageServer,
        [OSProduct.StorageStandardServerCore] = OSEdition.StandardStorageServerCoreInstallation,
        [OSProduct.StorageWorkgroupServer] = OSEdition.WorkgroupStorageServer,
        [OSProduct.StorageWorkgroupServerCore] = OSEdition.WorkgroupStorageServerCoreInstallation,
        [OSProduct.Undefined] = OSEdition.UnknownProduct,
        [OSProduct.Ultimate] = OSEdition.Ultimate,
        [OSProduct.UltimateN] = OSEdition.UltimateN,
        [OSProduct.UltimateE] = OSEdition.UltimateE,
        [OSProduct.WebServer] = OSEdition.WebServer,
        [OSProduct.WebServerCore] = OSEdition.WebServerCoreInstallation,
        [OSProduct.HomeServer] = OSEdition.HomeServer,
        [OSProduct.ToBeDefined] = OSEdition.Unknown,
        [OSProduct.Unlicensed] = OSEdition.Unknown
    };

    [DllImport("Kernel32.dll")]
    private static extern bool GetProductInfo(int osMajorVersion,
                                              int osMinorVersion,
                                              int spMajorVersion,
                                              int spMinorVersion,
                                              out uint edition);

    [DllImport("kernel32.dll")]
    private static extern bool GetVersionEx(ref OSVersionInfoEx osVersionInfo);

    [DllImport("user32")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("kernel32.dll")]
    // ReSharper disable UnusedMember.Local
    private static extern void GetSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SystemInfo lpSystemInfo);
    // ReSharper restore UnusedMember.Local

    [DllImport("kernel32.dll")]
    private static extern void GetNativeSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SystemInfo lpSystemInfo);

    private static OSProcessorArchitecture GetProcessorBits()
    {
        if (!IsWindows)
            return Is32Bit
                ? OSProcessorArchitecture.Bit32
                : OSProcessorArchitecture.Bit64;

        OSProcessorArchitecture pbits = OSProcessorArchitecture.Unknown;

        try
        {
            var lSystemInfo = new SystemInfo();
            GetNativeSystemInfo(ref lSystemInfo);

            pbits = lSystemInfo.uProcessorInfo.wProcessorArchitecture switch
            {
                // PROCESSOR_ARCHITECTURE_AMD64
                9 => OSProcessorArchitecture.Bit64,
                // PROCESSOR_ARCHITECTURE_IA64
                6 => OSProcessorArchitecture.Itanium64,
                // PROCESSOR_ARCHITECTURE_INTEL
                0 => OSProcessorArchitecture.Bit32,
                // PROCESSOR_ARCHITECTURE_UNKNOWN
                _ => OSProcessorArchitecture.Unknown
            };
        }
        catch
        {
            // Ignore
        }

        return pbits;
    }

    private static string GetServicePack()
    {
        if (!IsWindows)
            return null;

        var osVersionInfo = new OSVersionInfoEx
        {
            dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVersionInfoEx))
        };

        try
        {
            if (GetVersionEx(ref osVersionInfo))
                return osVersionInfo.szCSDVersion;
        }
        catch
        {
            //ignored
        }

        return null;
    }

    [SuppressMessage("Interoperability", "CA1416:Walidacja zgodności z platformą")]
    private static RegistryKey GetRegistryKey(string pathRoot)
    {
        if (!IsWindows)
            return null;

        return pathRoot.IsEqual("HKEY_CLASSES_ROOT") ? Registry.ClassesRoot :
            pathRoot.IsEqual("HKEY_CURRENT_USER") ? Registry.CurrentUser :
            pathRoot.IsEqual("HKEY_LOCAL_MACHINE") ? Registry.LocalMachine :
            pathRoot.IsEqual("HKEY_USERS") ? Registry.Users :
            pathRoot.IsEqual("HKEY_CURRENT_CONFIG") ? Registry.CurrentConfig : null;
    }

    private static string GetInfoString()
    {
        var sb = new StringBuilder();

        if (Name.IsNotNullOrEmptyString())
            sb.Append(Name);

        if (Edition != OSEdition.Unknown)
            sb.Append(' ').Append(Edition);

        if (Version is not null)
            sb.Append(' ').Append(Version);

        if (ProgramBits != SoftwareArchitecture.Unknown)
            sb.Append(' ').Append(ProgramBits);

        return sb.ToString();
    }

    private static SoftwareArchitecture GetProgramBits()
    {
        int check = IntPtr.Size * 8;

        return check switch
        {
            64 => SoftwareArchitecture.Bit64,
            32 => SoftwareArchitecture.Bit32,
            _ => SoftwareArchitecture.Unknown
        };
    }

    private static SoftwareArchitecture GetOSBits()
    {
        int check = IntPtr.Size * 8;

        return check switch
        {
            64 => SoftwareArchitecture.Bit64,
            32 => Environment.Is64BitOperatingSystem //Is 32-bit program on 64-bit OS
                ? SoftwareArchitecture.Bit64
                : SoftwareArchitecture.Bit32,
            _ => SoftwareArchitecture.Unknown
        };
    }

    private static OSEdition GetEdition()
    {
        if (!IsWindows)
            return OSEdition.Unknown;

        OperatingSystem osVersion = Environment.OSVersion;

        var osVersionInfo = new OSVersionInfoEx
        {
            dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVersionInfoEx))
        };

        try
        {
            if (GetVersionEx(ref osVersionInfo))
            {
                int majorVersion = osVersion.Version.Major;
                int minorVersion = osVersion.Version.Minor;
                byte productType = osVersionInfo.wProductType;
                short suiteMask = osVersionInfo.wSuiteMask;

                switch (majorVersion)
                {
                    case 4:
                        return productType switch
                        {
                            VerNtWorkstation => OSEdition.Workstation,
                            VerNtServer => (suiteMask & VerSuiteEnterprise) != 0
                                ? OSEdition.EnterpriseServer
                                : OSEdition.StandardServer,
                            _ => OSEdition.Unknown
                        };
                    case 5 when productType == VerNtWorkstation:
                        return (suiteMask & VerSuitePersonal) != 0 ? OSEdition.Home :
                            GetSystemMetrics(SmTabletPC) == 0 ? OSEdition.Professional : OSEdition.TabletEdition;
                    case 5 when productType == VerNtServer:
                        return minorVersion == 0
                            ?
                            (suiteMask & VerSuiteDatacenter) != 0 ? OSEdition.DatacenterServer :
                            (suiteMask & VerSuiteEnterprise) != 0 ? OSEdition.AdvancedServer : OSEdition.Server
                            : (suiteMask & VerSuiteDatacenter) != 0
                                ? OSEdition.Datacenter
                                : (suiteMask & VerSuiteEnterprise) != 0
                                    ? OSEdition.Enterprise
                                    : (suiteMask & VerSuiteBlade) != 0
                                        ? OSEdition.WebEdition
                                        : OSEdition.Standard;
                    case 5:
                        return OSEdition.Unknown;
                    case 6 when GetProductInfo(majorVersion, minorVersion, osVersionInfo.wServicePackMajor,
                        osVersionInfo.wServicePackMinor, out uint ed):
                        return GetEditionFromProduct(ed);
                }
            }
        }
        catch
        {
            //ignored
        }

        return OSEdition.Unknown;
    }

    private static OSEdition GetEditionFromProduct(uint product) => GetEditionFromProduct((OSProduct)product);

    private static OSEdition GetEditionFromProduct(OSProduct product) => ProductToEdition.TryGetKeyValue(product);

    [SuppressMessage("ReSharper", "CognitiveComplexity")]
    private static string GetName()
    {
        // if (IsWindows)
        //     return null;

        OperatingSystem osVersion = Environment.OSVersion;

        var osVersionInfo = new OSVersionInfoEx
        {
            dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVersionInfoEx))
        };

        try
        {
            if (GetVersionEx(ref osVersionInfo))
            {
                int majorVersion = osVersion.Version.Major;
                int minorVersion = osVersion.Version.Minor;

                if (majorVersion == 6
                    && minorVersion == 2)
                {
                    //The registry read workaround is by Scott Vickery. Thanks a lot for the help!

                    //http://msdn.microsoft.com/en-us/library/windows/desktop/ms724832(v=vs.85).aspx

                    // For applications that have been manifested for Windows 8.1 & Windows 10. Applications not manifested for 8.1 or 10 will return the Windows 8 OS version value (6.2).
                    // By reading the registry, we'll get the exact version - meaning we can even compare against  Win 8 and Win 8.1.
                    string exactVersion =
                        RegistryRead(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                            "CurrentVersion", "");

                    if (!string.IsNullOrEmpty(exactVersion))
                    {
                        string[] splitResult = exactVersion.Split('.');
                        majorVersion = Convert.ToInt32(splitResult[0]);
                        minorVersion = Convert.ToInt32(splitResult[1]);
                    }

                    if (IsWindows10())
                    {
                        majorVersion = 10;
                        minorVersion = 0;
                    }
                }

                switch (osVersion.Platform)
                {
                    case PlatformID.Win32S:
                        return "Windows 3.1";
                    case PlatformID.WinCE:
                        return "Windows CE";
                    case PlatformID.Win32Windows:
                    {
                        if (majorVersion == 4)
                        {
                            string csdVersion = osVersionInfo.szCSDVersion;

                            switch (minorVersion)
                            {
                                case 0:
                                    return csdVersion is "B" or "C"
                                        ? "Windows 95 OSR2"
                                        : "Windows 95";
                                case 10:
                                    return csdVersion == "A"
                                        ? "Windows 98 Second Edition"
                                        : "Windows 98";
                                case 90:
                                    return "Windows Me";
                            }
                        }

                        break;
                    }
                    case PlatformID.Win32NT:
                    {
                        int productType = osVersionInfo.wProductType;

                        return new OSVersion(majorVersion, minorVersion, productType).ToString();
                    }
                    case PlatformID.Unix:
                    case PlatformID.Xbox:
                    case PlatformID.MacOSX:
                        //todo
                        break;
                }
            }
        }
        catch
        {
            //ignored
        }

        return "unknown";
    }

    private static int GetBuildVersion()
    {
        if (!IsWindows)
            return 0;

        string version = RegistryRead(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "CurrentBuildNumber", null);

        return version is not null
            ? int.Parse(version)
            : 0;
    }

    private static Version GetVersion()
    {
        if (!IsWindows)
            return null;

        Version currentVersion = GetCurrentVersion();

        return new Version(currentVersion.Major, currentVersion.Minor, GetBuildVersion(), currentVersion.Revision);
    }

    private static Version GetCurrentVersion()
    {
        if (IsWindows10())
            return new Version(10, 0, 0, 0);

        int revision = Environment.OSVersion.Version.Revision;

        string exactVersion = RegistryRead(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "CurrentVersion", null);

        if (exactVersion.IsNotNullOrEmptyString())
        {
            string[] splitVersion = exactVersion.Split('.');

            return new Version(int.Parse(splitVersion[0]), int.Parse(splitVersion[1]), 0, revision);
        }

        return new Version(Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, 0, revision);
    }

    private static bool IsWindows10()
    {
        string productName = RegistryRead(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "ProductName", "");

        return productName?.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase) == true;
    }

    [SuppressMessage("Interoperability", "CA1416:Walidacja zgodności z platformą")]
    private static string RegistryRead(string registryPath, string field, string defaultValue)
    {
        string rtn = null;
        var backSlash = "";
        var newRegistryPath = "";

        try
        {
            string[] splitResult = registryPath.Split('\\');

            if (splitResult.Length > 0)
            {
                RegistryKey ourKey = GetRegistryKey(splitResult[0]);

                try
                {
                    if (ourKey is not null)
                    {
                        for (var i = 1; i < splitResult.Length; i++)
                        {
                            newRegistryPath += backSlash + splitResult[i];
                            backSlash = "\\";
                        }

                        if (newRegistryPath != "")
                        {
                            //rtn = (string)Registry.GetValue(RegistryPath, "CurrentVersion", DefaultValue);
#pragma warning disable IDISP016
#pragma warning disable IDISP007
                            ourKey.Dispose();
#pragma warning restore IDISP007
#pragma warning restore IDISP016
                            ourKey = ourKey.OpenSubKey(newRegistryPath);
                            rtn = (string)ourKey?.GetValue(field, defaultValue);
                            ourKey?.Close();
                        }
                    }
                }
                finally
                {
                    ourKey?.Dispose();
                }
            }
        }
        catch
        {
            // ignored
        }

        return rtn;
    }

    [SuppressMessage("ReSharper", "UnusedType.Local")]
    private delegate bool IsWow64ProcessDelegate([In] IntPtr handle, [Out] out bool isWow64Process);

    static OSVersionInfo()
    {
        ProgramBits = GetProgramBits();
        OSBits = GetOSBits();
        ProcessorBits = GetProcessorBits();
        Edition = GetEdition();
        Name = GetName();
        ServicePack = GetServicePack();
        BuildVersion = GetBuildVersion();
        Version = GetVersion();
        InfoString = GetInfoString();
    }

    // ReSharper disable UnusedMember.Local
    //todo convert to enum
    private const int VerNtWorkstation = 1;
    private const int VerNtDomainController = 2;
    private const int VerNtServer = 3;
    private const int VerSuiteSmallbusiness = 1;
    private const int VerSuiteEnterprise = 2;
    private const int VerSuiteTerminal = 16;
    private const int VerSuiteDatacenter = 128;
    private const int VerSuiteSingleuserts = 256;
    private const int VerSuitePersonal = 512;

    private const int VerSuiteBlade = 1024;
    // ReSharper restore UnusedMember.Local
}