using System.ComponentModel;

namespace FEx.Sqlx.Enums;

public enum ServerEngineEdition
{
    Unknown = 0,

    [Description("Personal or Desktop")]
    PersonalOrDesktopEngine = 1,

    [Description("Standard")]
    Standard = 2,

    [Description("Enterprise")]
    EnterpriseOrDeveloper = 3,

    [Description("Express")]
    Express = 4,

    [Description("SQL Database")]
    SqlDatabase = 5,

    [Description("Microsoft Azure Synapse Analytics")]
    SqlDataWarehouse = 6,
    SqlStretchDatabase = 7,

    [Description("Azure SQL Managed Instance")]
    SqlManagedInstance = 8,

    [Description("Azure SQL Edge")]
    AzureSQLEdge = 9,
    SqlOnDemand = 11
}