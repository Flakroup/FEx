using FEx.Sqlx.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Sqlx;

/// <summary>Default <see cref="ISqlInstanceProvider"/>: local SQL Server discovery through <see cref="SqlDbHelper"/>.</summary>
public class SqlInstanceProvider : ISqlInstanceProvider
{
    public async Task<IReadOnlyList<ISqlInstanceInfo>> GetSqlInstancesAsync() =>
        [.. await SqlDbHelper.GetSqlInstancesAsync()];
}
