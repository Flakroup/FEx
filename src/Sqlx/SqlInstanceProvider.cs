using FEx.Sqlx.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Sqlx;

/// <summary>
/// Default <see cref="ISqlInstanceProvider"/>, backed by <see cref="SqlDbHelper.GetSqlInstancesAsync"/>: the SQL Server
/// WMI provider on the local machine (SMO <c>ManagedComputer</c> on net481), falling back to the network
/// <c>SqlDataSourceEnumerator</c> (SSRP broadcast) where WMI lists nothing - so the result can contain remote instances.
/// </summary>
public class SqlInstanceProvider : ISqlInstanceProvider
{
    private readonly Func<Task<IList<SQLInstanceInfo>>> _discoverInstances;

    public SqlInstanceProvider()
        : this(SqlDbHelper.GetSqlInstancesAsync)
    {
    }

    // Seam for tests: the discovery source is injected so no machine-wide lookup is needed.
    internal SqlInstanceProvider(Func<Task<IList<SQLInstanceInfo>>> discoverInstances) =>
        _discoverInstances = discoverInstances;

    public async Task<IReadOnlyList<ISqlInstanceInfo>> GetSqlInstancesAsync() =>
        [.. await _discoverInstances()];
}
