using Dapper;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces.Collections;
using FEx.Sqlx.Enums;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsSqlConnection = Microsoft.Data.SqlClient.SqlConnection;

namespace FEx.Sqlx.Extensions;

// CS0618: System.Data.SqlClient.SqlConnection is obsolete in favour of Microsoft.Data.SqlClient.
// These overloads are intentionally retained for backward compatibility (the modern type is
// exposed via the MsSqlConnection overloads). Full migration tracked as tech debt.
#pragma warning disable CS0618

public static class SqlConnectionExtensions
{
    public static IMap<string, ServerProp> ServerProps { get; } = EnumExtensions.GetEnumMap<ServerProp>(true);

    public static IMap<long, string> ServerEditionIDs { get; } =
        EnumExtensions.GetEnumCustomMap<ServerEditionID, long, string>(x => (long)x,
            x => x.GetEnumValueDescription() ?? x.ToString());

    public static IMap<int, string> ServerEngineEditions { get; } =
        EnumExtensions.GetEnumCustomMap<ServerEngineEdition, int, string>(x => (int)x,
            x => x.GetEnumValueDescription() ?? x.ToString());

    internal static string PropsSQL { get; } = GetPropsSQL();

    public static async Task<IDictionary<string, object>[]> RunSqlAsync(this MsSqlConnection connection, string sql) =>
        (await connection.QueryAsync(sql)).Cast<IDictionary<string, object>>().ToArray();

    public static async Task<IDictionary<string, object>[]> RunSqlAsync(this SqlConnection connection, string sql) =>
        (await connection.QueryAsync(sql)).Cast<IDictionary<string, object>>().ToArray();

    public static IDictionary<string, object>[] RunSql(this MsSqlConnection connection, string sql) =>
        connection.Query(sql).Cast<IDictionary<string, object>>().ToArray();

    public static IDictionary<string, object>[] RunSql(this SqlConnection connection, string sql) =>
        connection.Query(sql).Cast<IDictionary<string, object>>().ToArray();

    public static async Task<IList<string>> LoadDatabasesAsync(this SqlConnection connection)
    {
        const string sql = "SELECT name, database_id, create_date  FROM sys.databases";

        return (await connection.RunSqlAsync(sql)).Select(x => Convert.ToString(x["name"])).ToArray();
    }

    public static async Task<Dictionary<string, long>> LoadTablesAsync(this SqlConnection connection)
    {
        const string sql = "select * from sys.tables order by name";
        var result = await connection.RunSqlAsync(sql);

        return result.ToDictionary(x => Convert.ToString(x["name"]),
            x => Convert.ToInt64(Convert.ToString(x["object_id"])));
    }

    public static async Task<(long tableId, string[] columnNames)> LoadColumnsAsync(
        this SqlConnection connection,
        long tableId,
        IProgress<bool> prg = null)
    {
        var sql = $"select name from sys.columns where object_id={tableId}";
        var result = await connection.RunSqlAsync(sql);
        prg?.Report(true);

        return (tableId, result.Select(x => Convert.ToString(x["name"])).ToArray());
    }

    private static string GetPropsSQL()
    {
        var sb = new StringBuilder().AppendLine("DECLARE @props TABLE (propertyname sysname PRIMARY KEY)")
            .AppendLine("INSERT INTO @props(propertyname)");

        for (var i = 0; i < ServerProps.ForwardIndex.Count; i++)
        {
            sb.Append("SELECT '").Append(ServerProps.ForwardIndex.ElementAt(i).Key).AppendLine("'");

            if (i != ServerProps.ForwardIndex.Count - 1)
                sb.AppendLine("UNION");
        }

        return sb.AppendLine()
            .AppendLine("SELECT propertyname, SERVERPROPERTY(propertyname) AS propertyvalue FROM @props")
            .ToString();
    }
}