using FEx.EFCore.Interfaces;

namespace FEx.EFCore.Configuration;

public class DbServiceConfig : IDbServiceConfig
{
    public IBulkDbConfig BulkDbConfig { get; }
    public IFExDbConfig DbConfig { get; }

    public DbServiceConfig(IFExDbConfig dbConfig, IBulkDbConfig bulkDbConfig = null)
    {
        BulkDbConfig = bulkDbConfig;
        DbConfig = dbConfig;
    }
}