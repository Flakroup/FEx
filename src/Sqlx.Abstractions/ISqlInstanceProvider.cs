using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Sqlx.Abstractions;

/// <summary>
/// Enumerates SQL Server instances. Implementations list the local machine's instances first (SQL Server WMI provider);
/// where that yields nothing, the default implementation falls back to a network (SSRP broadcast) lookup, so callers
/// must not treat a returned instance as local or trusted.
/// </summary>
public interface ISqlInstanceProvider
{
    Task<IReadOnlyList<ISqlInstanceInfo>> GetSqlInstancesAsync();
}
