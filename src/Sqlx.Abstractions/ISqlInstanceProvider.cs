using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Sqlx.Abstractions;

/// <summary>Enumerates the SQL Server instances available on the local machine.</summary>
public interface ISqlInstanceProvider
{
    Task<IReadOnlyList<ISqlInstanceInfo>> GetSqlInstancesAsync();
}
