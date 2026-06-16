using System.ComponentModel;

namespace FEx.Sqlx.Enums;

public enum ServerEditionID : long
{
    [Description("Enterprise")]
    Enterprise = 1804890536,

    [Description("Enterprise Edition: Core-based Licensing")]
    EnterpriseCore = 1872460670,

    [Description("Enterprise Evaluation")]
    EnterpriseEvaluation = 610778273,

    [Description("Business Intelligence")]
    BusinessIntelligence = 284895786,

    [Description("Developer")]
    Developer = -2117995310,

    [Description("Express")]
    Express = -1592396055,

    [Description("Express with Advanced Services")]
    ExpressAdvanced = -133711905,

    [Description("Standard")]
    Standard = -1534726760,

    [Description("Web")]
    Web = 1293598313,

    [Description("SQL Database or Azure Synapse Analytics (SQL Data Warehouse)")]
    SQLDataWarehouse = 1674378470,

    [Description("Azure SQL Edge Developer")]
    AzureSQLEdgeDeveloper = -1461570097,

    [Description("Azure SQL Edge")]
    AzureSQLEdge = 1994083197
}