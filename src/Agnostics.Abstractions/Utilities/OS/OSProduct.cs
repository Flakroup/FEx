namespace FEx.Agnostics.Abstractions.Utilities.OS;

/// <summary>Windows product editions as returned by the Win32 <c>GetProductInfo</c> API (<c>PRODUCT_*</c> values).</summary>
public enum OSProduct : uint
{
    /// <summary>Product could not be determined.</summary>
    Undefined = 0x00000000,
    /// <summary>Windows Ultimate edition.</summary>
    Ultimate = 0x00000001,
    /// <summary>Windows Home Basic edition.</summary>
    HomeBasic = 0x00000002,
    /// <summary>Windows Home Premium edition.</summary>
    HomePremium = 0x00000003,
    /// <summary>Windows Enterprise edition.</summary>
    Enterprise = 0x00000004,
    /// <summary>Windows Home Basic N edition.</summary>
    HomeBasicN = 0x00000005,
    /// <summary>Windows Business edition.</summary>
    Business = 0x00000006,
    /// <summary>Windows Standard Server edition.</summary>
    StandardServer = 0x00000007,
    /// <summary>Windows Datacenter Server edition.</summary>
    DatacenterServer = 0x00000008,
    /// <summary>Windows Small Business Server edition.</summary>
    SmallbusinessServer = 0x00000009,
    /// <summary>Windows Enterprise Server edition.</summary>
    EnterpriseServer = 0x0000000A,
    /// <summary>Windows Starter edition.</summary>
    Starter = 0x0000000B,
    /// <summary>Windows Datacenter Server (core installation) edition.</summary>
    DatacenterServerCore = 0x0000000C,
    /// <summary>Windows Standard Server (core installation) edition.</summary>
    StandardServerCore = 0x0000000D,
    /// <summary>Windows Enterprise Server (core installation) edition.</summary>
    EnterpriseServerCore = 0x0000000E,
    /// <summary>Windows Enterprise Server for Itanium edition.</summary>
    EnterpriseServerIa64 = 0x0000000F,
    /// <summary>Windows Business N edition.</summary>
    BusinessN = 0x00000010,
    /// <summary>Windows Web Server edition.</summary>
    WebServer = 0x00000011,
    /// <summary>Windows Cluster Server edition.</summary>
    ClusterServer = 0x00000012,
    /// <summary>Windows Home Server edition.</summary>
    HomeServer = 0x00000013,
    /// <summary>Windows Storage Express Server edition.</summary>
    StorageExpressServer = 0x00000014,
    /// <summary>Windows Storage Standard Server edition.</summary>
    StorageStandardServer = 0x00000015,
    /// <summary>Windows Storage Workgroup Server edition.</summary>
    StorageWorkgroupServer = 0x00000016,
    /// <summary>Windows Storage Enterprise Server edition.</summary>
    StorageEnterpriseServer = 0x00000017,
    /// <summary>Windows Server For Small Business edition.</summary>
    ServerForSmallbusiness = 0x00000018,
    /// <summary>Windows Small Business Server Premium edition.</summary>
    SmallbusinessServerPremium = 0x00000019,
    /// <summary>Windows Home Premium N edition.</summary>
    HomePremiumN = 0x0000001A,
    /// <summary>Windows Enterprise N edition.</summary>
    EnterpriseN = 0x0000001B,
    /// <summary>Windows Ultimate N edition.</summary>
    UltimateN = 0x0000001C,
    /// <summary>Windows Web Server (core installation) edition.</summary>
    WebServerCore = 0x0000001D,
    /// <summary>Windows Medium Business Server Management edition.</summary>
    MediumbusinessServerManagement = 0x0000001E,
    /// <summary>Windows Medium Business Server Security edition.</summary>
    MediumbusinessServerSecurity = 0x0000001F,
    /// <summary>Windows Medium Business Server Messaging edition.</summary>
    MediumbusinessServerMessaging = 0x00000020,
    /// <summary>Windows Server Foundation edition.</summary>
    ServerFoundation = 0x00000021,
    /// <summary>Windows Home Premium Server edition.</summary>
    HomePremiumServer = 0x00000022,
    /// <summary>Windows Server For Small Business without Hyper-V edition.</summary>
    ServerForSmallbusinessV = 0x00000023,
    /// <summary>Windows Standard Server without Hyper-V edition.</summary>
    StandardServerV = 0x00000024,
    /// <summary>Windows Datacenter Server without Hyper-V edition.</summary>
    DatacenterServerV = 0x00000025,
    /// <summary>Windows Enterprise Server without Hyper-V edition.</summary>
    EnterpriseServerV = 0x00000026,
    /// <summary>Windows Datacenter Server without Hyper-V (core installation) edition.</summary>
    DatacenterServerCoreV = 0x00000027,
    /// <summary>Windows Standard Server without Hyper-V (core installation) edition.</summary>
    StandardServerCoreV = 0x00000028,
    /// <summary>Windows Enterprise Server without Hyper-V (core installation) edition.</summary>
    EnterpriseServerCoreV = 0x00000029,
    /// <summary>Microsoft Hyper-V Server.</summary>
    Hyperv = 0x0000002A,
    /// <summary>Windows Storage Express Server (core installation) edition.</summary>
    StorageExpressServerCore = 0x0000002B,
    /// <summary>Windows Storage Standard Server (core installation) edition.</summary>
    StorageStandardServerCore = 0x0000002C,
    /// <summary>Windows Storage Workgroup Server (core installation) edition.</summary>
    StorageWorkgroupServerCore = 0x0000002D,
    /// <summary>Windows Storage Enterprise Server (core installation) edition.</summary>
    StorageEnterpriseServerCore = 0x0000002E,
    /// <summary>Windows Starter N edition.</summary>
    StarterN = 0x0000002F,
    /// <summary>Windows Professional edition.</summary>
    Professional = 0x00000030,
    /// <summary>Windows Professional N edition.</summary>
    ProfessionalN = 0x00000031,
    /// <summary>Windows SB Solution Server edition.</summary>
    SbSolutionServer = 0x00000032,
    /// <summary>Windows Server For SB Solutions edition.</summary>
    ServerForSbSolutions = 0x00000033,
    /// <summary>Windows Standard Server Solutions edition.</summary>
    StandardServerSolutions = 0x00000034,
    /// <summary>Windows Standard Server Solutions (core installation) edition.</summary>
    StandardServerSolutionsCore = 0x00000035,
    /// <summary>Windows SB Solution Server EM edition.</summary>
    SbSolutionServerEm = 0x00000036,
    /// <summary>Windows Server For SB Solutions EM edition.</summary>
    ServerForSbSolutionsEm = 0x00000037,
    /// <summary>Windows Solution EMbeddedserver edition.</summary>
    SolutionEmbeddedserver = 0x00000038,
    /// <summary>Windows Solution EMbeddedserver (core installation) edition.</summary>
    SolutionEmbeddedserverCore = 0x00000039,
    /// <summary>Placeholder value reserved for a product that is yet to be defined.</summary>
    ToBeDefined = 0x0000003A,
    /// <summary>Windows Essential Business Server MGMT edition.</summary>
    EssentialbusinessServerMgmt = 0x0000003B,
    /// <summary>Windows Essential Business Server ADDL edition.</summary>
    EssentialbusinessServerAddl = 0x0000003C,
    /// <summary>Windows Essential Business Server MGMTSVC edition.</summary>
    EssentialbusinessServerMgmtsvc = 0x0000003D,
    /// <summary>Windows Essential Business Server ADDLSVC edition.</summary>
    EssentialbusinessServerAddlsvc = 0x0000003E,
    /// <summary>Windows Small Business Server Premium (core installation) edition.</summary>
    SmallbusinessServerPremiumCore = 0x0000003F,
    /// <summary>Windows Cluster Server without Hyper-V edition.</summary>
    ClusterServerV = 0x00000040,
    /// <summary>Windows Embedded edition.</summary>
    Embedded = 0x00000041,
    /// <summary>Windows Starter E edition.</summary>
    StarterE = 0x00000042,
    /// <summary>Windows Home Basic E edition.</summary>
    HomeBasicE = 0x00000043,
    /// <summary>Windows Home Premium E edition.</summary>
    HomePremiumE = 0x00000044,
    /// <summary>Windows Professional E edition.</summary>
    ProfessionalE = 0x00000045,
    /// <summary>Windows Enterprise E edition.</summary>
    EnterpriseE = 0x00000046,
    /// <summary>Windows Ultimate E edition.</summary>
    UltimateE = 0x00000047,
    /// <summary>Windows is not licensed (unlicensed product marker).</summary>
    Unlicensed = 0xABCDABCD
}