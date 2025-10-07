using System.ComponentModel;

namespace FEx.Common.Utilities.OS.Enums;

public enum OSEdition
{
    Unknown = 0,

    /// <summary>
    /// Windows NT 4.0 Workstation
    /// </summary>
    [Description("Workstation")]
    Workstation,

    /// <summary>
    /// Windows NT 4.0 and higher Server Enterprise
    /// </summary>
    [Description("Enterprise Server")]
    EnterpriseServer,

    /// <summary>
    /// Windows NT 4.0 and higher Server
    /// </summary>
    [Description("Standard Server")]
    StandardServer,

    /// <summary>
    /// Home
    /// </summary>
    [Description("Home")]
    Home,

    /// <summary>
    /// Professional
    /// </summary>
    [Description("Professional")]
    Professional,

    /// <summary>
    /// Tablet Edition
    /// </summary>
    [Description("Tablet Edition")]
    TabletEdition,

    /// <summary>
    /// Windows 2000 and higher Datacenter Server
    /// </summary>
    [Description("Datacenter Server")]
    DatacenterServer,

    /// <summary>
    /// Windows 2000 Advanced Server
    /// </summary>
    [Description("Advanced Server")]
    AdvancedServer,

    /// <summary>
    /// Windows 2000 Server
    /// </summary>
    [Description("Server")]
    Server,

    /// <summary>
    /// Windows Server 2003 Datacenter Edition
    /// </summary>
    [Description("Datacenter")]
    Datacenter,

    /// <summary>
    /// Windows Server 2003 and higher Enterprise Edition
    /// </summary>
    [Description("Enterprise")]
    Enterprise,

    /// <summary>
    /// Windows Server 2003 Web Edition
    /// </summary>
    [Description("Web Edition")]
    WebEdition,

    /// <summary>
    /// Windows Server 2003 Standard Edition
    /// </summary>
    [Description("Standard")]
    Standard,

    /// <summary>
    /// Business
    /// </summary>
    [Description("Business")]
    Business,

    /// <summary>
    /// Business N
    /// </summary>
    [Description("Business N")]
    BusinessN,

    /// <summary>
    /// HPC Edition
    /// </summary>
    [Description("HPC Edition")]
    HPCEdition,

    /// <summary>
    /// HPC Edition without Hyper-V
    /// </summary>
    [Description("HPC Edition without Hyper-V")]
    HPCEditionWithoutHyperV,

    /// <summary>
    /// Datacenter Server (core installation)
    /// </summary>
    [Description("Datacenter Server (core installation)")]
    DatacenterServerCoreInstallation,

    /// <summary>
    /// Datacenter Server without Hyper-V
    /// </summary>
    [Description("Datacenter Server without Hyper-V")]
    DatacenterServerWithoutHyperV,

    /// <summary>
    /// Datacenter Server without Hyper-V (core installation)
    /// </summary>
    [Description("Datacenter Server without Hyper-V (core installation)")]
    DatacenterServerWithoutHyperVCoreInstallation,

    /// <summary>
    /// Embedded
    /// </summary>
    [Description("Embedded")]
    Embedded,

    /// <summary>
    /// Enterprise N
    /// </summary>
    [Description("Enterprise N")]
    EnterpriseN,

    /// <summary>
    /// Enterprise E
    /// </summary>
    [Description("Enterprise E")]
    EnterpriseE,

    /// <summary>
    /// Enterprise Server (core installation)
    /// </summary>
    [Description("Enterprise Server (core installation)")]
    EnterpriseServerCoreInstallation,

    /// <summary>
    /// Enterprise Server without Hyper-V (core installation)
    /// </summary>
    [Description("Enterprise Server without Hyper-V (core installation)")]
    EnterpriseServerWithoutHyperVCoreInstallation,

    /// <summary>
    /// Enterprise Server for Itanium-based Systems
    /// </summary>
    [Description("Enterprise Server for Itanium-based Systems)")]
    EnterpriseServerForItaniumBasedSystems,

    /// <summary>
    /// Enterprise Server without Hyper-V
    /// </summary>
    [Description("Enterprise Server without Hyper-V")]
    EnterpriseServerWithoutHyperV,

    /// <summary>
    /// Essential Business Server MGMT
    /// </summary>
    [Description("Essential Business Server MGMT")]
    EssentialBusinessServerMGMT,

    /// <summary>
    /// Essential Business Server ADDL
    /// </summary>
    [Description("Essential Business Server ADDL")]
    EssentialBusinessServerADDL,

    /// <summary>
    /// Essential Business Server MGMTSVC
    /// </summary>
    [Description("Essential Business Server MGMTSVC")]
    EssentialBusinessServerMGMTSVC,

    /// <summary>
    /// Essential Business Server ADDLSVC
    /// </summary>
    [Description("Essential Business Server ADDLSVC")]
    EssentialBusinessServerADDLSVC,

    /// <summary>
    /// Home Basic
    /// </summary>
    [Description("Home Basic")]
    HomeBasic,

    /// <summary>
    /// Home Basic N
    /// </summary>
    [Description("Home Basic N")]
    HomeBasicN,

    /// <summary>
    /// Home Basic E
    /// </summary>
    [Description("Home Basic E")]
    HomeBasicE,

    /// <summary>
    /// Home Premium
    /// </summary>
    [Description("Home Premium")]
    HomePremium,

    /// <summary>
    /// Home Premium N
    /// </summary>
    [Description("Home Premium N")]
    HomePremiumN,

    /// <summary>
    /// Home Premium E
    /// </summary>
    [Description("Home Premium E")]
    HomePremiumE,

    /// <summary>
    /// Home Premium Server
    /// </summary>
    [Description("Home Premium Server")]
    HomePremiumServer,

    /// <summary>
    /// Microsoft Hyper-V Server
    /// </summary>
    [Description("Microsoft Hyper-V Server")]
    MicrosoftHyperVServer,

    /// <summary>
    /// Windows Essential Business Management Server
    /// </summary>
    [Description("Windows Essential Business Management Server")]
    WindowsEssentialBusinessManagementServer,

    /// <summary>
    /// Windows Essential Business Messaging Server
    /// </summary>
    [Description("Windows Essential Business Messaging Server")]
    WindowsEssentialBusinessMessagingServer,

    /// <summary>
    /// Windows Essential Business Security Server
    /// </summary>
    [Description("Windows Essential Business Security Server")]
    WindowsEssentialBusinessSecurityServer,

    /// <summary>
    /// Professional N
    /// </summary>
    [Description("Professional N")]
    ProfessionalN,

    /// <summary>
    /// Professional E
    /// </summary>
    [Description("Professional E")]
    ProfessionalE,

    /// <summary>
    /// SB Solution Server
    /// </summary>
    [Description("SB Solution Server")]
    SBSolutionServer,

    /// <summary>
    /// SB Solution Server EM
    /// </summary>
    [Description("SB Solution Server EM")]
    SBSolutionServerEM,

    /// <summary>
    /// Server for SB Solutions
    /// </summary>
    [Description("Server for SB Solutions")]
    ServerForSBSolutions,

    /// <summary>
    /// Server for SB Solutions EM
    /// </summary>
    [Description("Server for SB Solutions EM")]
    ServerForSBSolutionsEM,

    /// <summary>
    /// Windows Essential Server Solutions
    /// </summary>
    [Description("Windows Essential Server Solutions")]
    WindowsEssentialServerSolutions,

    /// <summary>
    /// Windows Essential Server Solutions without Hyper-V
    /// </summary>
    [Description("Windows Essential Server Solutions without Hyper-V")]
    WindowsEssentialServerSolutionsWithoutHyperV,

    /// <summary>
    /// Server Foundation
    /// </summary>
    [Description("Server Foundation")]
    ServerFoundation,

    /// <summary>
    /// Windows Small Business Server
    /// </summary>
    [Description("Windows Small Business Server")]
    WindowsSmallBusinessServer,

    /// <summary>
    /// Windows Small Business Server Premium
    /// </summary>
    [Description("Windows Small Business Server Premium")]
    WindowsSmallBusinessServerPremium,

    /// <summary>
    /// Windows Small Business Server Premium (core installation)
    /// </summary>
    [Description("Windows Small Business Server Premium (core installation)")]
    WindowsSmallBusinessServerPremiumCoreInstallation,

    /// <summary>
    /// Solution Embedded Server
    /// </summary>
    [Description("Solution Embedded Server")]
    SolutionEmbeddedServer,

    /// <summary>
    /// Solution Embedded Server (core installation)
    /// </summary>
    [Description("Solution Embedded Server (core installation)")]
    SolutionEmbeddedServerCoreInstallation,

    /// <summary>
    /// Standard Server (core installation)
    /// </summary>
    [Description("Standard Server (core installation)")]
    StandardServerCoreInstallation,

    /// <summary>
    /// Standard Server Solutions
    /// </summary>
    [Description("Standard Server Solutions")]
    StandardServerSolutions,

    /// <summary>
    /// Standard Server Solutions (core installation)
    /// </summary>
    [Description("Standard Server Solutions (core installation)")]
    StandardServerSolutionsCoreInstallation,

    /// <summary>
    /// Standard Server without Hyper-V (core installation)
    /// </summary>
    [Description("Standard Server without Hyper-V (core installation)")]
    StandardServerWithoutHyperVCoreInstallation,

    /// <summary>
    /// Standard Server without Hyper-V
    /// </summary>
    [Description("Standard Server without Hyper-V")]
    StandardServerWithoutHyperV,

    /// <summary>
    /// Starter
    /// </summary>
    [Description("Starter")]
    Starter,

    /// <summary>
    /// Starter N
    /// </summary>
    [Description("Starter N")]
    StarterN,

    /// <summary>
    /// Starter E
    /// </summary>
    [Description("Starter E")]
    StarterE,

    /// <summary>
    /// Enterprise Storage Server
    /// </summary>
    [Description("Enterprise Storage Server")]
    EnterpriseStorageServer,

    /// <summary>
    /// Enterprise Storage Server (core installation)
    /// </summary>
    [Description("Enterprise Storage Server (core installation)")]
    EnterpriseStorageServerCoreInstallation,

    /// <summary>
    /// Express Storage Server
    /// </summary>
    [Description("Express Storage Server")]
    ExpressStorageServer,

    /// <summary>
    /// Express Storage Server (core installation)
    /// </summary>
    [Description("Express Storage Server (core installation)")]
    ExpressStorageServerCoreInstallation,

    /// <summary>
    /// Standard Storage Server
    /// </summary>
    [Description("Standard Storage Server")]
    StandardStorageServer,

    /// <summary>
    /// Standard Storage Server (core installation)
    /// </summary>
    [Description("Standard Storage Server (core installation)")]
    StandardStorageServerCoreInstallation,

    /// <summary>
    /// Workgroup Storage Server
    /// </summary>
    [Description("Workgroup Storage Server")]
    WorkgroupStorageServer,

    /// <summary>
    /// Workgroup Storage Server (core installation)
    /// </summary>
    [Description("Workgroup Storage Server (core installation)")]
    WorkgroupStorageServerCoreInstallation,

    /// <summary>
    /// Unknown product
    /// </summary>
    [Description("Unknown product")]
    UnknownProduct,

    /// <summary>
    /// Ultimate
    /// </summary>
    [Description("Ultimate")]
    Ultimate,

    /// <summary>
    /// Ultimate N
    /// </summary>
    [Description("Ultimate N")]
    UltimateN,

    /// <summary>
    /// Ultimate E
    /// </summary>
    [Description("Ultimate E")]
    UltimateE,

    /// <summary>
    /// Web Server
    /// </summary>
    [Description("Web Server")]
    WebServer,

    /// <summary>
    /// Web Server (core installation)
    /// </summary>
    [Description("Web Server (core installation)")]
    WebServerCoreInstallation,

    /// <summary>
    /// Home Server
    /// </summary>
    [Description("Home Server")]
    HomeServer
}