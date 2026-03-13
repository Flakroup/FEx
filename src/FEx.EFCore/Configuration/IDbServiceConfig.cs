using FEx.EFCore.Interfaces;

namespace FEx.EFCore.Configuration;

public interface IDbServiceConfig
{
    IBulkDbConfig BulkDbConfig { get; }
    IFExDbConfig DbConfig { get; }
}